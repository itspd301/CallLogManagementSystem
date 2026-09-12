using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.CallLog;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Services
{
    public class CallLogService : ICallLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICallNumberService _callNumberService;
        private readonly IAuditService _auditService;

        public CallLogService(ApplicationDbContext context, ICallNumberService callNumberService, IAuditService auditService)
        {
            _context = context;
            _callNumberService = callNumberService;
            _auditService = auditService;
        }

        public async Task<CallLogCreateResult> CreateAsync(CreateCallLogViewModel model, string createdByUserId)
        {
            // Section 34 — domain validation beyond simple [Required] attributes.
            var shopBelongsToLocation = await _context.Shops.AnyAsync(s => s.Id == model.ShopId && s.LocationId == model.LocationId);
            if (!shopBelongsToLocation)
            {
                return CallLogCreateResult.Failure(nameof(model.ShopId), "The selected Shop does not belong to the selected Location.");
            }

            if (model.IsLineLoss)
            {
                if (string.IsNullOrWhiteSpace(model.LineLossArea))
                {
                    return CallLogCreateResult.Failure(nameof(model.LineLossArea), "Line/Area is required when Line Loss is Yes.");
                }

                if (model.LineLossStartDateTime == null)
                {
                    return CallLogCreateResult.Failure(nameof(model.LineLossStartDateTime), "Downtime Start is required when Line Loss is Yes.");
                }

                if (model.LineLossEndDateTime.HasValue && model.LineLossEndDateTime < model.LineLossStartDateTime)
                {
                    return CallLogCreateResult.Failure(nameof(model.LineLossEndDateTime), "Downtime End cannot be before Downtime Start.");
                }

                if (!model.LineLossAffectedVehicles.HasValue || model.LineLossAffectedVehicles < 1)
                {
                    return CallLogCreateResult.Failure(nameof(model.LineLossAffectedVehicles), "Affected Vehicles is required when Line Loss is Yes.");
                }
            }

            var openStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == CallStatusCodes.Open);
            if (openStatus == null)
            {
                return CallLogCreateResult.Failure(string.Empty, "The 'Open' status is not configured. Contact your administrator.");
            }

            int? lineLossDuration = null;
            if (model.IsLineLoss && model.LineLossStartDateTime.HasValue && model.LineLossEndDateTime.HasValue)
            {
                lineLossDuration = (int)(model.LineLossEndDateTime.Value - model.LineLossStartDateTime.Value).TotalMinutes;
            }

            var callNumber = await _callNumberService.GenerateAsync();

            var callLog = new CallLog
            {
                CallNumber = callNumber,
                LocationId = model.LocationId,
                ShopId = model.ShopId,
                CallType = model.CallType,
                ModuleId = model.ModuleId,
                ApplicationTypeId = model.ApplicationTypeId,
                ProblemCategoryId = model.ProblemCategoryId,
                ProblemId = model.ProblemId,
                ReportedById = model.ReportedById,
                ReportedDateTime = model.ReportedDateTime,
                Description = model.Description.Trim(),
                CallCategoryId = model.CallCategoryId,
                PriorityId = model.PriorityId,
                StatusId = openStatus.Id,
                ICAPCA = string.IsNullOrWhiteSpace(model.ICAPCA) ? null : model.ICAPCA.Trim(),
                AttendedById = model.AttendedById,
                HandedOverToId = model.HandedOverToId,
                IsLineLoss = model.IsLineLoss,
                LineLossArea = model.IsLineLoss ? model.LineLossArea!.Trim() : null,
                LineLossStartDateTime = model.IsLineLoss ? model.LineLossStartDateTime : null,
                LineLossEndDateTime = model.IsLineLoss ? model.LineLossEndDateTime : null,
                LineLossDurationMinutes = lineLossDuration,
                LineLossAffectedVehicles = model.IsLineLoss ? model.LineLossAffectedVehicles : null,
                Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim(),
                CreatedById = createdByUserId,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            };

            _context.CallLogs.Add(callLog);
            await _context.SaveChangesAsync();

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = callLog.Id,
                ActivityType = ActivityType.CallCreated,
                Description = $"Call {callNumber} created.",
                PerformedById = createdByUserId,
                PerformedDateTime = DateTime.Now,
                NewStatusId = openStatus.Id
            });
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.CallCreated, nameof(CallLog), callLog.Id.ToString(), newValue: callNumber, userId: createdByUserId);

            return CallLogCreateResult.Success(callLog);
        }

        public async Task<CallLog?> GetByIdAsync(int id)
        {
            return await _context.CallLogs
                .Include(c => c.Location)
                .Include(c => c.Shop)
                .Include(c => c.Module)
                .Include(c => c.ApplicationType)
                .Include(c => c.ProblemCategory)
                .Include(c => c.Problem)
                .Include(c => c.ReportedBy)
                .Include(c => c.CallCategory)
                .Include(c => c.Priority)
                .Include(c => c.Status)
                .Include(c => c.AttendedBy!).ThenInclude(e => e!.ApplicationUser)
                .Include(c => c.HandedOverTo!).ThenInclude(e => e!.ApplicationUser)
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CallLogUpdateResult> UpdateAsync(int id, EditCallLogViewModel model, string performedByUserId, bool canEditSpecialFields)
        {
            var callLog = await _context.CallLogs.FirstOrDefaultAsync(c => c.Id == id);
            if (callLog == null)
            {
                return CallLogUpdateResult.Failure(string.Empty, "Call not found.");
            }

            // Section 17 — closed calls are read-only except to Admin/Support Manager, the same
            // tier that holds the special-fields permission.
            var closedStatus = await _context.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.Code == CallStatusCodes.Closed);
            var isClosed = closedStatus != null && callLog.StatusId == closedStatus.Id;
            if (isClosed && !canEditSpecialFields)
            {
                return CallLogUpdateResult.Failure(string.Empty, "This call is closed and can only be edited by an Administrator or Support Manager.");
            }

            if (canEditSpecialFields)
            {
                var shopBelongsToLocation = await _context.Shops.AnyAsync(s => s.Id == model.ShopId && s.LocationId == model.LocationId);
                if (!shopBelongsToLocation)
                {
                    return CallLogUpdateResult.Failure(nameof(model.ShopId), "The selected Shop does not belong to the selected Location.");
                }

                if (model.IsLineLoss)
                {
                    if (string.IsNullOrWhiteSpace(model.LineLossArea))
                    {
                        return CallLogUpdateResult.Failure(nameof(model.LineLossArea), "Line/Area is required when Line Loss is Yes.");
                    }

                    if (model.LineLossStartDateTime == null)
                    {
                        return CallLogUpdateResult.Failure(nameof(model.LineLossStartDateTime), "Downtime Start is required when Line Loss is Yes.");
                    }

                    if (model.LineLossEndDateTime.HasValue && model.LineLossEndDateTime < model.LineLossStartDateTime)
                    {
                        return CallLogUpdateResult.Failure(nameof(model.LineLossEndDateTime), "Downtime End cannot be before Downtime Start.");
                    }

                    if (!model.LineLossAffectedVehicles.HasValue || model.LineLossAffectedVehicles < 1)
                    {
                        return CallLogUpdateResult.Failure(nameof(model.LineLossAffectedVehicles), "Affected Vehicles is required when Line Loss is Yes.");
                    }
                }
            }

            var changes = new List<string>();
            var activities = new List<CallActivity>();

            async Task TrackAsync(string label, string? oldValue, string? newValue)
            {
                if (oldValue == newValue)
                {
                    return;
                }

                changes.Add($"{label}: \"{oldValue ?? "-"}\" → \"{newValue ?? "-"}\"");
                activities.Add(new CallActivity
                {
                    CallLogId = callLog.Id,
                    ActivityType = ActivityType.FieldUpdated,
                    Description = $"{label} updated.",
                    OldValue = oldValue,
                    NewValue = newValue,
                    PerformedById = performedByUserId,
                    PerformedDateTime = DateTime.Now
                });
                await Task.CompletedTask;
            }

            // --- Normally editable fields (every permitted editor, including the attending engineer) ---
            await TrackAsync("Description", callLog.Description, model.Description.Trim());
            callLog.Description = model.Description.Trim();

            if (callLog.ProblemCategoryId != model.ProblemCategoryId)
            {
                var oldName = await _context.ProblemCategories.AsNoTracking().Where(x => x.Id == callLog.ProblemCategoryId).Select(x => x.Name).FirstOrDefaultAsync();
                var newName = await _context.ProblemCategories.AsNoTracking().Where(x => x.Id == model.ProblemCategoryId).Select(x => x.Name).FirstOrDefaultAsync();
                await TrackAsync("Problem Category", oldName, newName);
                callLog.ProblemCategoryId = model.ProblemCategoryId;
            }

            if (callLog.ProblemId != model.ProblemId)
            {
                var oldName = callLog.ProblemId == null ? null : await _context.Problems.AsNoTracking().Where(x => x.Id == callLog.ProblemId).Select(x => x.Name).FirstOrDefaultAsync();
                var newName = model.ProblemId == null ? null : await _context.Problems.AsNoTracking().Where(x => x.Id == model.ProblemId).Select(x => x.Name).FirstOrDefaultAsync();
                await TrackAsync("Problem", oldName, newName);
                callLog.ProblemId = model.ProblemId;
            }

            if (callLog.CallCategoryId != model.CallCategoryId)
            {
                var oldName = await _context.CallCategories.AsNoTracking().Where(x => x.Id == callLog.CallCategoryId).Select(x => x.Name).FirstOrDefaultAsync();
                var newName = await _context.CallCategories.AsNoTracking().Where(x => x.Id == model.CallCategoryId).Select(x => x.Name).FirstOrDefaultAsync();
                await TrackAsync("Call Category", oldName, newName);
                callLog.CallCategoryId = model.CallCategoryId;
            }

            if (callLog.PriorityId != model.PriorityId)
            {
                var oldName = await _context.Priorities.AsNoTracking().Where(x => x.Id == callLog.PriorityId).Select(x => x.Name).FirstOrDefaultAsync();
                var newName = await _context.Priorities.AsNoTracking().Where(x => x.Id == model.PriorityId).Select(x => x.Name).FirstOrDefaultAsync();
                await TrackAsync("Priority", oldName, newName);
                callLog.PriorityId = model.PriorityId;
            }

            var newIcaPca = string.IsNullOrWhiteSpace(model.ICAPCA) ? null : model.ICAPCA.Trim();
            await TrackAsync("ICA/PCA", callLog.ICAPCA, newIcaPca);
            callLog.ICAPCA = newIcaPca;

            var newRemarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
            await TrackAsync("Remarks", callLog.Remarks, newRemarks);
            callLog.Remarks = newRemarks;

            // --- Special-permission fields (Admin / Support Manager only) ---
            if (canEditSpecialFields)
            {
                if (callLog.LocationId != model.LocationId)
                {
                    var oldName = await _context.Locations.AsNoTracking().Where(x => x.Id == callLog.LocationId).Select(x => x.Name).FirstOrDefaultAsync();
                    var newName = await _context.Locations.AsNoTracking().Where(x => x.Id == model.LocationId).Select(x => x.Name).FirstOrDefaultAsync();
                    await TrackAsync("Location", oldName, newName);
                    callLog.LocationId = model.LocationId;
                }

                if (callLog.ShopId != model.ShopId)
                {
                    var oldName = await _context.Shops.AsNoTracking().Where(x => x.Id == callLog.ShopId).Select(x => x.Name).FirstOrDefaultAsync();
                    var newName = await _context.Shops.AsNoTracking().Where(x => x.Id == model.ShopId).Select(x => x.Name).FirstOrDefaultAsync();
                    await TrackAsync("Shop", oldName, newName);
                    callLog.ShopId = model.ShopId;
                }

                if (callLog.ModuleId != model.ModuleId)
                {
                    var oldName = await _context.Modules.AsNoTracking().Where(x => x.Id == callLog.ModuleId).Select(x => x.Name).FirstOrDefaultAsync();
                    var newName = await _context.Modules.AsNoTracking().Where(x => x.Id == model.ModuleId).Select(x => x.Name).FirstOrDefaultAsync();
                    await TrackAsync("Module", oldName, newName);
                    callLog.ModuleId = model.ModuleId;
                }

                if (callLog.ApplicationTypeId != model.ApplicationTypeId)
                {
                    var oldName = callLog.ApplicationTypeId == null ? null : await _context.ApplicationTypes.AsNoTracking().Where(x => x.Id == callLog.ApplicationTypeId).Select(x => x.Name).FirstOrDefaultAsync();
                    var newName = model.ApplicationTypeId == null ? null : await _context.ApplicationTypes.AsNoTracking().Where(x => x.Id == model.ApplicationTypeId).Select(x => x.Name).FirstOrDefaultAsync();
                    await TrackAsync("Application Type", oldName, newName);
                    callLog.ApplicationTypeId = model.ApplicationTypeId;
                }

                if (callLog.ReportedById != model.ReportedById)
                {
                    var oldName = await _context.Employees.AsNoTracking().Where(x => x.Id == callLog.ReportedById).Select(x => x.FullName).FirstOrDefaultAsync();
                    var newName = await _context.Employees.AsNoTracking().Where(x => x.Id == model.ReportedById).Select(x => x.FullName).FirstOrDefaultAsync();
                    await TrackAsync("Problem Reported By", oldName, newName);
                    callLog.ReportedById = model.ReportedById;
                }

                if (callLog.ReportedDateTime != model.ReportedDateTime)
                {
                    await TrackAsync("Reported Date & Time", callLog.ReportedDateTime.ToString("g"), model.ReportedDateTime.ToString("g"));
                    callLog.ReportedDateTime = model.ReportedDateTime;
                }

                if (callLog.AttendedById != model.AttendedById)
                {
                    // Logged once as EngineerChanged (Section 19's dedicated activity type for
                    // this), not also as a generic FieldUpdated — one timeline entry, not two.
                    var oldName = await _context.Engineers.AsNoTracking().Where(x => x.Id == callLog.AttendedById).Select(x => x.ApplicationUser!.FullName).FirstOrDefaultAsync();
                    var newName = await _context.Engineers.AsNoTracking().Where(x => x.Id == model.AttendedById).Select(x => x.ApplicationUser!.FullName).FirstOrDefaultAsync();
                    changes.Add($"Attended By: \"{oldName ?? "-"}\" → \"{newName ?? "-"}\"");
                    activities.Add(new CallActivity
                    {
                        CallLogId = callLog.Id,
                        ActivityType = ActivityType.EngineerChanged,
                        Description = $"Attended By changed from {oldName ?? "-"} to {newName ?? "-"}.",
                        OldValue = oldName,
                        NewValue = newName,
                        PerformedById = performedByUserId,
                        PerformedDateTime = DateTime.Now
                    });
                    callLog.AttendedById = model.AttendedById;
                }

                if (callLog.IsLineLoss != model.IsLineLoss)
                {
                    await TrackAsync("Is Line Loss", callLog.IsLineLoss ? "Yes" : "No", model.IsLineLoss ? "Yes" : "No");
                    callLog.IsLineLoss = model.IsLineLoss;
                }

                var newLineLossArea = model.IsLineLoss ? model.LineLossArea?.Trim() : null;
                await TrackAsync("Line/Area", callLog.LineLossArea, newLineLossArea);
                callLog.LineLossArea = newLineLossArea;

                var newStart = model.IsLineLoss ? model.LineLossStartDateTime : null;
                if (callLog.LineLossStartDateTime != newStart)
                {
                    await TrackAsync("Downtime Start", callLog.LineLossStartDateTime?.ToString("g"), newStart?.ToString("g"));
                    callLog.LineLossStartDateTime = newStart;
                }

                var newEnd = model.IsLineLoss ? model.LineLossEndDateTime : null;
                if (callLog.LineLossEndDateTime != newEnd)
                {
                    await TrackAsync("Downtime End", callLog.LineLossEndDateTime?.ToString("g"), newEnd?.ToString("g"));
                    callLog.LineLossEndDateTime = newEnd;
                }

                callLog.LineLossDurationMinutes = callLog is { IsLineLoss: true, LineLossStartDateTime: not null, LineLossEndDateTime: not null }
                    ? (int)(callLog.LineLossEndDateTime!.Value - callLog.LineLossStartDateTime!.Value).TotalMinutes
                    : null;

                var newAffectedVehicles = model.IsLineLoss ? model.LineLossAffectedVehicles : null;
                if (callLog.LineLossAffectedVehicles != newAffectedVehicles)
                {
                    await TrackAsync("Affected Vehicles", callLog.LineLossAffectedVehicles?.ToString(), newAffectedVehicles?.ToString());
                    callLog.LineLossAffectedVehicles = newAffectedVehicles;
                }
            }

            if (changes.Count == 0)
            {
                return CallLogUpdateResult.Success(changes);
            }

            callLog.ModifiedById = performedByUserId;
            callLog.ModifiedDate = DateTime.Now;

            _context.CallActivities.AddRange(activities);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.CallEdited, nameof(CallLog), callLog.Id.ToString(),
                oldValue: null, newValue: string.Join("; ", changes), userId: performedByUserId);

            return CallLogUpdateResult.Success(changes);
        }
    }
}
