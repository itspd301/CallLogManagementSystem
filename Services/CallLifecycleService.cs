using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Services
{
    public class CallLifecycleService : ICallLifecycleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public CallLifecycleService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<LifecycleResult> StartProgressAsync(int callLogId, string performedByUserId)
        {
            var callLog = await LoadAsync(callLogId);
            if (callLog == null) return LifecycleResult.Failure("Call not found.");

            var currentCode = callLog.Status?.Code;
            if (currentCode != CallStatusCodes.Assigned && currentCode != CallStatusCodes.OnHold && currentCode != CallStatusCodes.Reopened)
            {
                return LifecycleResult.Failure($"A call must be Assigned, On Hold or Reopened to start progress (currently {callLog.Status?.Name}).");
            }

            return await TransitionAsync(callLog, CallStatusCodes.InProgress, performedByUserId,
                extra: null);
        }

        public async Task<LifecycleResult> ResolveAsync(int callLogId, string icaPca, string? remarks, string performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(icaPca))
            {
                return LifecycleResult.Failure("ICA/PCA (root cause and corrective action) is required to resolve a call.");
            }

            var callLog = await LoadAsync(callLogId);
            if (callLog == null) return LifecycleResult.Failure("Call not found.");

            var currentCode = callLog.Status?.Code;
            if (currentCode != CallStatusCodes.Assigned && currentCode != CallStatusCodes.InProgress && currentCode != CallStatusCodes.OnHold)
            {
                return LifecycleResult.Failure($"Only an Assigned, In Progress or On Hold call can be resolved (currently {callLog.Status?.Name}).");
            }

            callLog.ICAPCA = icaPca.Trim();
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                callLog.Remarks = remarks.Trim();
            }

            return await TransitionAsync(callLog, CallStatusCodes.Resolved, performedByUserId,
                extra: new CallActivity
                {
                    CallLogId = callLog.Id,
                    ActivityType = ActivityType.CallResolved,
                    Description = "Call resolved.",
                    NewValue = icaPca.Trim(),
                    PerformedById = performedByUserId,
                    PerformedDateTime = DateTime.Now
                });
        }

        public async Task<LifecycleResult> CloseAsync(int callLogId, string? remarks, string performedByUserId)
        {
            var callLog = await LoadAsync(callLogId);
            if (callLog == null) return LifecycleResult.Failure("Call not found.");

            if (callLog.Status?.Code != CallStatusCodes.Resolved)
            {
                return LifecycleResult.Failure($"Only a Resolved call can be closed (currently {callLog.Status?.Name}).");
            }

            // Section 9/34 — Closing Date & Time and Time Taken are system-controlled, computed
            // here, never accepted from the client.
            var closingDateTime = DateTime.Now;
            callLog.ClosingDateTime = closingDateTime;
            callLog.TimeTakenMinutes = Math.Max(0, (int)(closingDateTime - callLog.ReportedDateTime).TotalMinutes);

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                callLog.Remarks = remarks.Trim();
            }

            return await TransitionAsync(callLog, CallStatusCodes.Closed, performedByUserId,
                extra: new CallActivity
                {
                    CallLogId = callLog.Id,
                    ActivityType = ActivityType.CallClosed,
                    Description = $"Call closed. Time taken: {callLog.TimeTakenMinutes} minutes.",
                    PerformedById = performedByUserId,
                    PerformedDateTime = DateTime.Now
                });
        }

        public async Task<LifecycleResult> ReopenAsync(int callLogId, string reason, string performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return LifecycleResult.Failure("A reason is required to reopen a call.");
            }

            var callLog = await LoadAsync(callLogId);
            if (callLog == null) return LifecycleResult.Failure("Call not found.");

            if (callLog.Status?.Code != CallStatusCodes.Closed)
            {
                return LifecycleResult.Failure($"Only a Closed call can be reopened (currently {callLog.Status?.Name}).");
            }

            // Section 34 — a non-Closed call should normally have no ClosingDateTime; clearing
            // both here so a reopened call's Time Taken reflects only its next resolution.
            callLog.ClosingDateTime = null;
            callLog.TimeTakenMinutes = null;

            var result = await TransitionAsync(callLog, CallStatusCodes.Reopened, performedByUserId,
                extra: new CallActivity
                {
                    CallLogId = callLog.Id,
                    ActivityType = ActivityType.CallReopened,
                    Description = $"Call reopened. Reason: {reason.Trim()}",
                    PerformedById = performedByUserId,
                    PerformedDateTime = DateTime.Now
                });

            if (result.Succeeded)
            {
                var attendedByUserId = await _context.Engineers.AsNoTracking()
                    .Where(e => e.Id == callLog.AttendedById)
                    .Select(e => e.ApplicationUserId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(attendedByUserId) && attendedByUserId != performedByUserId)
                {
                    await _notificationService.CreateAsync(attendedByUserId, callLog.Id, NotificationType.Reopened,
                        $"Call {callLog.CallNumber} was reopened.");
                }
            }

            return result;
        }

        private async Task<CallLog?> LoadAsync(int callLogId)
        {
            return await _context.CallLogs.Include(c => c.Status).FirstOrDefaultAsync(c => c.Id == callLogId);
        }

        private async Task<LifecycleResult> TransitionAsync(CallLog callLog, string newStatusCode, string performedByUserId, CallActivity? extra)
        {
            var newStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == newStatusCode);
            if (newStatus == null)
            {
                return LifecycleResult.Failure($"The '{newStatusCode}' status is not configured. Contact your administrator.");
            }

            var oldStatusId = callLog.StatusId;
            var oldStatusName = callLog.Status?.Name;
            callLog.StatusId = newStatus.Id;
            callLog.ModifiedById = performedByUserId;
            callLog.ModifiedDate = DateTime.Now;

            if (extra != null)
            {
                _context.CallActivities.Add(extra);
            }

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = callLog.Id,
                ActivityType = ActivityType.StatusChanged,
                Description = $"Status changed from {oldStatusName} to {newStatus.Name}.",
                OldStatusId = oldStatusId,
                NewStatusId = newStatus.Id,
                PerformedById = performedByUserId,
                PerformedDateTime = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.StatusChanged, nameof(CallLog), callLog.Id.ToString(),
                oldValue: oldStatusName, newValue: newStatus.Name, userId: performedByUserId);

            return LifecycleResult.Success();
        }
    }
}
