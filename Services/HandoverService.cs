using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Services
{
    public class HandoverService : IHandoverService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public HandoverService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<HandoverResult> HandoverAsync(int callLogId, int toEngineerId, string reason, string? remarks, string performedByUserId)
        {
            var callLog = await _context.CallLogs
                .Include(c => c.Status)
                .Include(c => c.AttendedBy!).ThenInclude(e => e!.ApplicationUser)
                .FirstOrDefaultAsync(c => c.Id == callLogId);

            if (callLog == null)
            {
                return HandoverResult.Failure("Call not found.");
            }

            if (callLog.Status != null && (callLog.Status.Code == CallStatusCodes.Closed || callLog.Status.Code == CallStatusCodes.Cancelled))
            {
                return HandoverResult.Failure($"This call is {callLog.Status.Name} and cannot be handed over.");
            }

            var fromEngineerId = callLog.AttendedById;

            if (fromEngineerId == toEngineerId)
            {
                return HandoverResult.Failure("This call is already attended by the selected engineer.");
            }

            var toEngineer = await _context.Engineers.AsNoTracking()
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.Id == toEngineerId && e.IsActive);

            if (toEngineer == null)
            {
                return HandoverResult.Failure("The selected engineer is not valid or is inactive.");
            }

            var fromEngineerName = callLog.AttendedBy?.ApplicationUser?.FullName ?? "-";
            var toEngineerName = toEngineer.ApplicationUser!.FullName;

            _context.CallHandovers.Add(new CallHandover
            {
                CallLogId = callLogId,
                FromEngineerId = fromEngineerId,
                ToEngineerId = toEngineerId,
                Reason = reason.Trim(),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
                HandoverDateTime = DateTime.Now,
                PerformedById = performedByUserId
            });

            // AttendedBy and HandedOverTo both move to the new engineer — AttendedBy reflects
            // who is actually working it now, HandedOverTo mirrors that as the most recent
            // handover target; the CallHandovers table keeps the full chain for history.
            callLog.AttendedById = toEngineerId;
            callLog.HandedOverToId = toEngineerId;
            callLog.ModifiedById = performedByUserId;
            callLog.ModifiedDate = DateTime.Now;

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = callLogId,
                ActivityType = ActivityType.CallHandedOver,
                Description = $"Handed over from {fromEngineerName} to {toEngineerName}. Reason: {reason.Trim()}",
                OldValue = fromEngineerName,
                NewValue = toEngineerName,
                PerformedById = performedByUserId,
                PerformedDateTime = DateTime.Now,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.EngineerChanged, nameof(CallLog), callLogId.ToString(),
                oldValue: fromEngineerName, newValue: toEngineerName, userId: performedByUserId);

            if (toEngineer.ApplicationUserId != performedByUserId)
            {
                await _notificationService.CreateAsync(toEngineer.ApplicationUserId, callLogId, NotificationType.HandedOver,
                    $"Call {callLog.CallNumber} was handed over to you.");
            }

            return HandoverResult.Success();
        }
    }
}
