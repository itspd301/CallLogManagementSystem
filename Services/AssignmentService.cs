using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public AssignmentService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<AssignmentResult> AssignAsync(int callLogId, int engineerId, string? remarks, string performedByUserId)
        {
            var callLog = await _context.CallLogs
                .Include(c => c.Status)
                .Include(c => c.AttendedBy!).ThenInclude(e => e!.ApplicationUser)
                .FirstOrDefaultAsync(c => c.Id == callLogId);

            if (callLog == null)
            {
                return AssignmentResult.Failure("Call not found.");
            }

            if (callLog.Status != null && (callLog.Status.Code == CallStatusCodes.Closed || callLog.Status.Code == CallStatusCodes.Cancelled))
            {
                return AssignmentResult.Failure($"This call is {callLog.Status.Name} and cannot be (re)assigned.");
            }

            var engineer = await _context.Engineers.AsNoTracking()
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.Id == engineerId && e.IsActive);

            if (engineer == null)
            {
                return AssignmentResult.Failure("The selected engineer is not valid or is inactive.");
            }

            var previousAttendedByName = callLog.AttendedBy?.ApplicationUser?.FullName;
            var isReassignment = callLog.AttendedById != 0 && callLog.AttendedById != engineerId;

            _context.CallAssignments.Add(new CallAssignment
            {
                CallLogId = callLogId,
                EngineerId = engineerId,
                AssignedById = performedByUserId,
                AssignedDateTime = DateTime.Now,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            });

            callLog.AttendedById = engineerId;
            callLog.ModifiedById = performedByUserId;
            callLog.ModifiedDate = DateTime.Now;

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = callLogId,
                ActivityType = isReassignment ? ActivityType.EngineerChanged : ActivityType.EngineerAssigned,
                Description = isReassignment
                    ? $"Reassigned from {previousAttendedByName ?? "-"} to {engineer.ApplicationUser!.FullName}."
                    : $"Assigned to {engineer.ApplicationUser!.FullName}.",
                OldValue = previousAttendedByName,
                NewValue = engineer.ApplicationUser!.FullName,
                PerformedById = performedByUserId,
                PerformedDateTime = DateTime.Now,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            });

            // Section 8 — assigning an engineer to a freshly Open call advances it to Assigned.
            // Reassigning a call already further along the workflow (In Progress, On Hold, ...)
            // doesn't push it backward.
            if (callLog.Status?.Code == CallStatusCodes.Open)
            {
                var assignedStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == CallStatusCodes.Assigned);
                if (assignedStatus != null)
                {
                    var oldStatusId = callLog.StatusId;
                    callLog.StatusId = assignedStatus.Id;

                    _context.CallActivities.Add(new CallActivity
                    {
                        CallLogId = callLogId,
                        ActivityType = ActivityType.StatusChanged,
                        Description = $"Status changed from {callLog.Status.Name} to {assignedStatus.Name}.",
                        OldStatusId = oldStatusId,
                        NewStatusId = assignedStatus.Id,
                        PerformedById = performedByUserId,
                        PerformedDateTime = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.EngineerChanged, nameof(CallLog), callLogId.ToString(),
                oldValue: previousAttendedByName, newValue: engineer.ApplicationUser!.FullName, userId: performedByUserId);

            if (engineer.ApplicationUserId != performedByUserId)
            {
                await _notificationService.CreateAsync(engineer.ApplicationUserId, callLogId, NotificationType.Assigned,
                    $"Call {callLog.CallNumber} was assigned to you.");
            }

            return AssignmentResult.Success();
        }
    }
}
