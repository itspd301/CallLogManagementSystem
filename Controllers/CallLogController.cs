using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.CallLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    public class CallLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICallLogService _callLogService;
        private readonly IAttachmentService _attachmentService;
        private readonly IAssignmentService _assignmentService;
        private readonly IHandoverService _handoverService;
        private readonly ICallLifecycleService _lifecycleService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CallLogController> _logger;

        public CallLogController(
            ApplicationDbContext context,
            ICallLogService callLogService,
            IAttachmentService attachmentService,
            IAssignmentService assignmentService,
            IHandoverService handoverService,
            ICallLifecycleService lifecycleService,
            ICurrentUserService currentUser,
            ILogger<CallLogController> logger)
        {
            _context = context;
            _callLogService = callLogService;
            _attachmentService = attachmentService;
            _assignmentService = assignmentService;
            _handoverService = handoverService;
            _lifecycleService = lifecycleService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [Authorize(Policy = PolicyNames.ViewAllCalls)]
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, string? status, int? statusId, int? priorityId, int? locationId, int? shopId, int? moduleId,
            DateTime? dateFrom, DateTime? dateTo, string sort = "ReportedDateTime", string dir = "desc", int page = 1, int pageSize = 25)
        {
            var baseQuery = _context.CallLogs.AsNoTracking().AsQueryable();
            string pageTitle = "All Calls";

            // Sidebar shortcuts (Open/Closed/Reopened Calls) resolve to a StatusId here rather
            // than the client guessing one, since Status is admin-configurable master data.
            if (!string.IsNullOrWhiteSpace(status))
            {
                var resolvedStatus = await _context.Statuses.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Code == status.ToUpperInvariant());
                if (resolvedStatus != null)
                {
                    statusId = resolvedStatus.Id;
                    pageTitle = resolvedStatus.Name + " Calls";
                }
            }

            var vm = await BuildListViewModelAsync(baseQuery, search, statusId, status, priorityId, locationId, shopId, moduleId,
                dateFrom, dateTo, sort, dir, page, pageSize);
            vm.PageTitle = pageTitle;

            return View(vm);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyCalls(
            string? search, int? statusId, int? priorityId, int? shopId, int? moduleId,
            DateTime? dateFrom, DateTime? dateTo, string sort = "ReportedDateTime", string dir = "desc", int page = 1, int pageSize = 25)
        {
            var currentUserId = _currentUser.UserId!;
            var myEngineerId = await _context.Engineers.AsNoTracking()
                .Where(e => e.ApplicationUserId == currentUserId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            // Section 13 — union of every relationship the current user has to a call.
            var baseQuery = _context.CallLogs.AsNoTracking().Where(c =>
                c.CreatedById == currentUserId ||
                (myEngineerId != null && c.AttendedById == myEngineerId) ||
                (myEngineerId != null && c.HandedOverToId == myEngineerId) ||
                (myEngineerId != null && c.Assignments.Any(a => a.EngineerId == myEngineerId)));

            var vm = await BuildListViewModelAsync(baseQuery, search, statusId, null, priorityId, null, shopId, moduleId,
                dateFrom, dateTo, sort, dir, page, pageSize);
            vm.PageTitle = "My Calls";
            vm.ShowRelationColumn = true;

            // One extra query for the whole page (not per row) to know which of these calls
            // have an assignment to the current engineer.
            var pageCallLogIds = vm.Items.Select(i => i.Id).ToList();
            var assignedCallLogIds = myEngineerId == null
                ? new HashSet<int>()
                : (await _context.CallAssignments.AsNoTracking()
                    .Where(a => a.EngineerId == myEngineerId && pageCallLogIds.Contains(a.CallLogId))
                    .Select(a => a.CallLogId)
                    .ToListAsync()).ToHashSet();

            foreach (var row in vm.Items)
            {
                if (row.CreatedById == currentUserId) row.RelationTags.Add("Created");
                if (myEngineerId != null && row.AttendedById == myEngineerId) row.RelationTags.Add("Attended");
                if (myEngineerId != null && row.HandedOverToId == myEngineerId) row.RelationTags.Add("Handed Over");
                if (assignedCallLogIds.Contains(row.Id)) row.RelationTags.Add("Assigned");
            }

            return View("Index", vm);
        }

        // ------------------------------------------------------------------
        // Phase 15 — Call Details page (Section 18).
        // ------------------------------------------------------------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var callLog = await _callLogService.GetByIdAsync(id);
            if (callLog == null)
            {
                return NotFound();
            }

            var (_, canEditNormal, _) = await GetEditPermissionsAsync(callLog);
            var canAddCommentOrAttachment = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager) ||
                                    _currentUser.IsInRole(RoleNames.SupportEngineer) || _currentUser.IsInRole(RoleNames.Supervisor);

            var timeTakenDisplay = callLog.TimeTakenMinutes.HasValue
                ? $"{callLog.TimeTakenMinutes / 60 / 24}d {callLog.TimeTakenMinutes / 60 % 24}h {callLog.TimeTakenMinutes % 60}m"
                : null;

            var isPrivilegedForAttachments = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            var currentUserIdForAttachments = _currentUser.UserId;

            var attachments = await _context.CallAttachments.AsNoTracking()
                .Where(a => a.CallLogId == id)
                .Include(a => a.UploadedBy)
                .OrderByDescending(a => a.UploadedDate)
                .Select(a => new AttachmentVm
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedByName = a.UploadedBy!.FullName,
                    UploadedDate = a.UploadedDate,
                    CanDelete = isPrivilegedForAttachments || a.UploadedById == currentUserIdForAttachments
                })
                .ToListAsync();

            var comments = await _context.CallComments.AsNoTracking()
                .Where(c => c.CallLogId == id)
                .Include(c => c.CreatedBy)
                .OrderBy(c => c.CreatedDate)
                .Select(c => new CommentVm
                {
                    Comment = c.Comment,
                    CreatedByName = c.CreatedBy!.FullName,
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            var activities = await _context.CallActivities.AsNoTracking()
                .Where(a => a.CallLogId == id)
                .Include(a => a.PerformedBy)
                .OrderBy(a => a.PerformedDateTime)
                .Select(a => new ActivityVm
                {
                    TypeLabel = a.ActivityType.ToString(),
                    Description = a.Description,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    PerformedByName = a.PerformedBy!.FullName,
                    PerformedDateTime = a.PerformedDateTime
                })
                .ToListAsync();

            var assignmentHistory = await _context.CallAssignments.AsNoTracking()
                .Where(a => a.CallLogId == id)
                .Include(a => a.Engineer!).ThenInclude(e => e!.ApplicationUser)
                .Include(a => a.AssignedBy)
                .OrderByDescending(a => a.AssignedDateTime)
                .Select(a => new AssignmentHistoryVm
                {
                    EngineerName = a.Engineer!.ApplicationUser!.FullName,
                    AssignedByName = a.AssignedBy!.FullName,
                    AssignedDateTime = a.AssignedDateTime,
                    Remarks = a.Remarks
                })
                .ToListAsync();

            var canAssign = (_currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager)) &&
                             callLog.Status?.Code != CallStatusCodes.Closed && callLog.Status?.Code != CallStatusCodes.Cancelled;

            var handoverHistory = await _context.CallHandovers.AsNoTracking()
                .Where(h => h.CallLogId == id)
                .Include(h => h.FromEngineer!).ThenInclude(e => e!.ApplicationUser)
                .Include(h => h.ToEngineer!).ThenInclude(e => e!.ApplicationUser)
                .Include(h => h.PerformedBy)
                .OrderByDescending(h => h.HandoverDateTime)
                .Select(h => new HandoverHistoryVm
                {
                    FromEngineerName = h.FromEngineer!.ApplicationUser!.FullName,
                    ToEngineerName = h.ToEngineer!.ApplicationUser!.FullName,
                    Reason = h.Reason,
                    Remarks = h.Remarks,
                    PerformedByName = h.PerformedBy!.FullName,
                    HandoverDateTime = h.HandoverDateTime
                })
                .ToListAsync();

            // Section 7 — Admin/Support Manager can hand over any open call; a Support Engineer
            // may only hand over a call they are currently attending ("where permitted").
            var isNotTerminal = callLog.Status?.Code != CallStatusCodes.Closed && callLog.Status?.Code != CallStatusCodes.Cancelled;
            var canHandoverAny = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            var canHandover = isNotTerminal && (canHandoverAny || (canEditNormal && callLog.AttendedBy?.ApplicationUserId == _currentUser.UserId));

            // Section 8 lifecycle — Start Progress / Resolve open to Admin, Support Manager, or
            // the attending Support Engineer (Section 7: "Resolve calls" is an explicit
            // Support Engineer capability, scoped to their own assigned call). Close/Reopen are
            // Admin/Support Manager/Supervisor only — no role grants Support Engineer either.
            var statusCode = callLog.Status?.Code;
            var isAttendingEngineer = callLog.AttendedBy?.ApplicationUserId == _currentUser.UserId;
            var canProgressCalls = (_currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager) ||
                                     (_currentUser.IsInRole(RoleNames.SupportEngineer) && isAttendingEngineer));
            var canCloseOrReopen = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager) || _currentUser.IsInRole(RoleNames.Supervisor);

            var canStartProgress = canProgressCalls && (statusCode == CallStatusCodes.Assigned || statusCode == CallStatusCodes.OnHold || statusCode == CallStatusCodes.Reopened);
            var canResolve = canProgressCalls && (statusCode == CallStatusCodes.Assigned || statusCode == CallStatusCodes.InProgress || statusCode == CallStatusCodes.OnHold);
            var canClose = canCloseOrReopen && statusCode == CallStatusCodes.Resolved;
            var canReopen = canCloseOrReopen && statusCode == CallStatusCodes.Closed;

            // Quick action for calls fixed on the spot: Assign (if still Open) / Start Progress
            // (if Reopened) → Resolve → Close in one step. Requires the combined authority of
            // both underlying actions (Assign is Admin/Support Manager only, Close excludes
            // Support Engineer), so it's narrower than either individual permission alone.
            var canResolveAndClose = canHandoverAny && statusCode is CallStatusCodes.Open or CallStatusCodes.Assigned
                or CallStatusCodes.InProgress or CallStatusCodes.OnHold or CallStatusCodes.Reopened;

            var model = new CallLogDetailsViewModel
            {
                Id = callLog.Id,
                CallNumber = callLog.CallNumber,
                PriorityName = callLog.Priority?.Name ?? "-",
                PriorityColor = callLog.Priority?.ColorCode,
                StatusName = callLog.Status?.Name ?? "-",
                StatusColor = callLog.Status?.ColorCode,
                StatusCode = statusCode ?? "-",
                LocationName = callLog.Location?.Name ?? "-",
                ShopName = callLog.Shop?.Name ?? "-",
                CallType = callLog.CallType,
                ModuleName = callLog.Module?.Name ?? "-",
                ApplicationTypeName = callLog.ApplicationType?.Name,
                ProblemCategoryName = callLog.ProblemCategory?.Name ?? "-",
                ProblemName = callLog.Problem?.Name,
                ReportedByName = callLog.ReportedBy?.FullName ?? "-",
                ReportedDateTime = callLog.ReportedDateTime,
                Description = callLog.Description,
                CallCategoryName = callLog.CallCategory?.Name ?? "-",
                AttendedByName = callLog.AttendedBy?.ApplicationUser?.FullName ?? "-",
                HandedOverToName = callLog.HandedOverTo?.ApplicationUser?.FullName,
                ICAPCA = callLog.ICAPCA,
                Remarks = callLog.Remarks,
                IsLineLoss = callLog.IsLineLoss,
                LineLossArea = callLog.LineLossArea,
                LineLossStartDateTime = callLog.LineLossStartDateTime,
                LineLossEndDateTime = callLog.LineLossEndDateTime,
                LineLossDurationMinutes = callLog.LineLossDurationMinutes,
                LineLossAffectedVehicles = callLog.LineLossAffectedVehicles,
                ClosingDateTime = callLog.ClosingDateTime,
                TimeTakenMinutes = callLog.TimeTakenMinutes,
                CreatedByName = callLog.CreatedBy?.FullName ?? "-",
                CreatedDate = callLog.CreatedDate,
                ModifiedByName = callLog.ModifiedBy?.FullName,
                ModifiedDate = callLog.ModifiedDate,
                Attachments = attachments,
                Comments = comments,
                Activities = activities,
                AssignmentHistory = assignmentHistory,
                HandoverHistory = handoverHistory,
                CanEdit = canEditNormal,
                CanAddCommentOrAttachment = canAddCommentOrAttachment,
                CanAssign = canAssign,
                CanHandover = canHandover,
                AssignForm = new AssignEngineerViewModel
                {
                    CallLogId = callLog.Id,
                    CallNumber = callLog.CallNumber,
                    CurrentAttendedByName = callLog.AttendedBy?.ApplicationUser?.FullName ?? "-",
                    EngineerOptions = await _context.Engineers.AsNoTracking().Where(e => e.IsActive)
                        .Include(e => e.ApplicationUser).OrderBy(e => e.ApplicationUser!.FullName)
                        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName })
                        .ToListAsync()
                },
                HandoverForm = new HandoverViewModel
                {
                    CallLogId = callLog.Id,
                    CallNumber = callLog.CallNumber,
                    FromEngineerName = callLog.AttendedBy?.ApplicationUser?.FullName ?? "-",
                    EngineerOptions = await _context.Engineers.AsNoTracking()
                        .Where(e => e.IsActive && e.Id != callLog.AttendedById)
                        .Include(e => e.ApplicationUser).OrderBy(e => e.ApplicationUser!.FullName)
                        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName })
                        .ToListAsync()
                },
                CanStartProgress = canStartProgress,
                CanResolve = canResolve,
                CanClose = canClose,
                CanReopen = canReopen,
                CanResolveAndClose = canResolveAndClose,
                ResolveForm = new ResolveCallViewModel { ICAPCA = callLog.ICAPCA },
                ResolveAndCloseForm = new ResolveCallViewModel { ICAPCA = callLog.ICAPCA }
            };

            return View(model);
        }

        [Authorize(Policy = PolicyNames.HandoverCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Handover(int id, HandoverViewModel model)
        {
            var callLog = await _context.CallLogs
                .Include(c => c.AttendedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (callLog == null)
            {
                return NotFound();
            }

            // Server-side resource check mirrors Details' CanHandover: Admin/Support Manager can
            // hand over any call; a Support Engineer only one they are currently attending.
            var canHandoverAny = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            if (!canHandoverAny && callLog.AttendedBy?.ApplicationUserId != _currentUser.UserId)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                TempData["HandoverError"] = "A reason is required for handover.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _handoverService.HandoverAsync(id, model.ToEngineerId, model.Reason, model.Remarks, _currentUser.UserId!);

            if (!result.Succeeded)
            {
                TempData["HandoverError"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["StatusMessage"] = "Call handed over successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------
        // Call Lifecycle — Start Progress / Resolve / Close / Reopen (Section 8).
        // Authorization here is resource-based (mirrors Handover): the policy attribute checks
        // "does this role ever get to do this", the in-method check adds "on THIS call, right now".
        // ------------------------------------------------------------------

        [Authorize(Policy = PolicyNames.ResolveCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProgress(int id)
        {
            var callLog = await _context.CallLogs.Include(c => c.AttendedBy).FirstOrDefaultAsync(c => c.Id == id);
            if (callLog == null) return NotFound();

            if (!CanActOnOwnAssignedCall(callLog))
            {
                return Forbid();
            }

            var result = await _lifecycleService.StartProgressAsync(id, _currentUser.UserId!);
            TempData[result.Succeeded ? "StatusMessage" : "LifecycleError"] = result.Succeeded ? "Call moved to In Progress." : result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = PolicyNames.ResolveCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, ResolveCallViewModel model)
        {
            var callLog = await _context.CallLogs.Include(c => c.AttendedBy).FirstOrDefaultAsync(c => c.Id == id);
            if (callLog == null) return NotFound();

            if (!CanActOnOwnAssignedCall(callLog))
            {
                return Forbid();
            }

            var result = await _lifecycleService.ResolveAsync(id, model.ICAPCA ?? string.Empty, model.Remarks, _currentUser.UserId!);
            TempData[result.Succeeded ? "StatusMessage" : "LifecycleError"] = result.Succeeded ? "Call resolved." : result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = PolicyNames.CloseCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, string? remarks)
        {
            var result = await _lifecycleService.CloseAsync(id, remarks, _currentUser.UserId!);
            TempData[result.Succeeded ? "StatusMessage" : "LifecycleError"] = result.Succeeded ? "Call closed." : result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = PolicyNames.CloseCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id, string reason)
        {
            var result = await _lifecycleService.ReopenAsync(id, reason, _currentUser.UserId!);
            TempData[result.Succeeded ? "StatusMessage" : "LifecycleError"] = result.Succeeded ? "Call reopened." : result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        // Quick action for calls resolved on the spot: Assign (if Open) / Start Progress (if
        // Reopened) → Resolve → Close in one submit. Admin/Support Manager only — narrower than
        // either underlying action alone, since it needs both Assign's and Close's authority.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveAndClose(int id, ResolveCallViewModel model)
        {
            if (!_currentUser.IsInRole(RoleNames.Admin) && !_currentUser.IsInRole(RoleNames.SupportManager))
            {
                return Forbid();
            }

            var callLog = await _context.CallLogs.Include(c => c.Status).FirstOrDefaultAsync(c => c.Id == id);
            if (callLog == null) return NotFound();

            if (callLog.Status?.Code == CallStatusCodes.Open)
            {
                var assignResult = await _assignmentService.AssignAsync(id, callLog.AttendedById, null, _currentUser.UserId!);
                if (!assignResult.Succeeded)
                {
                    TempData["LifecycleError"] = assignResult.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            else if (callLog.Status?.Code == CallStatusCodes.Reopened)
            {
                var startResult = await _lifecycleService.StartProgressAsync(id, _currentUser.UserId!);
                if (!startResult.Succeeded)
                {
                    TempData["LifecycleError"] = startResult.Error;
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            var resolveResult = await _lifecycleService.ResolveAsync(id, model.ICAPCA ?? string.Empty, model.Remarks, _currentUser.UserId!);
            if (!resolveResult.Succeeded)
            {
                TempData["LifecycleError"] = resolveResult.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            var closeResult = await _lifecycleService.CloseAsync(id, model.Remarks, _currentUser.UserId!);
            if (!closeResult.Succeeded)
            {
                TempData["LifecycleError"] = closeResult.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["StatusMessage"] = "Call resolved and closed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Admin/Support Manager act on any call; a Support Engineer only on one they're
        // currently attending — same resource-based rule as Handover.
        private bool CanActOnOwnAssignedCall(Models.Entities.CallManagement.CallLog callLog)
        {
            if (_currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager))
            {
                return true;
            }

            return _currentUser.IsInRole(RoleNames.SupportEngineer) && callLog.AttendedBy?.ApplicationUserId == _currentUser.UserId;
        }

        [Authorize(Policy = PolicyNames.AssignEngineer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, AssignEngineerViewModel model)
        {
            if (model.EngineerId <= 0)
            {
                TempData["StatusMessage"] = null;
                TempData["AssignError"] = "Please select an engineer to assign.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _assignmentService.AssignAsync(id, model.EngineerId, model.Remarks, _currentUser.UserId!);

            if (!result.Succeeded)
            {
                TempData["AssignError"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["StatusMessage"] = "Engineer assigned successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = PolicyNames.AddCommentOrAttachment)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttachment(int id, List<IFormFile> files)
        {
            var callLog = await _context.CallLogs.FirstOrDefaultAsync(c => c.Id == id);
            if (callLog == null)
            {
                return NotFound();
            }

            if (files is { Count: > 0 })
            {
                var currentUserId = _currentUser.UserId!;
                try
                {
                    var attachments = await _attachmentService.SaveAsync(id, files, currentUserId);
                    if (attachments.Count > 0)
                    {
                        _context.CallAttachments.AddRange(attachments);

                        foreach (var attachment in attachments)
                        {
                            _context.CallActivities.Add(new CallActivity
                            {
                                CallLogId = id,
                                ActivityType = ActivityType.AttachmentAdded,
                                Description = $"Attachment \"{attachment.FileName}\" added.",
                                PerformedById = currentUserId,
                                PerformedDateTime = DateTime.Now
                            });
                        }

                        await _context.SaveChangesAsync();
                        TempData["StatusMessage"] = $"{attachments.Count} attachment(s) added.";
                    }
                }
                catch (InvalidOperationException ex)
                {
                    TempData["AttachmentWarning"] = ex.Message;
                }
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Section 23 — deletion is resource-based: Admin/Support Manager can remove any
        // attachment, everyone else only the ones they uploaded themselves.
        [Authorize(Policy = PolicyNames.AddCommentOrAttachment)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
        {
            var attachment = await _context.CallAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId && a.CallLogId == id);
            if (attachment == null)
            {
                return NotFound();
            }

            var isPrivileged = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            if (!isPrivileged && attachment.UploadedById != _currentUser.UserId)
            {
                return Forbid();
            }

            var fileName = attachment.FileName;
            var result = await _attachmentService.DeleteAsync(attachmentId);

            if (!result.Succeeded)
            {
                TempData["AttachmentWarning"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = id,
                ActivityType = ActivityType.AttachmentRemoved,
                Description = $"Attachment \"{fileName}\" removed.",
                PerformedById = _currentUser.UserId!,
                PerformedDateTime = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Attachment removed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Section 22/38 — posted via fetch() from the Details page; the comment is appended to
        // the DOM without a full page reload. Returns JSON, not a redirect.
        [Authorize(Policy = PolicyNames.AddCommentOrAttachment)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return Json(new { success = false, error = "Comment cannot be empty." });
            }

            var callLogExists = await _context.CallLogs.AnyAsync(c => c.Id == id);
            if (!callLogExists)
            {
                return Json(new { success = false, error = "Call not found." });
            }

            var currentUserId = _currentUser.UserId!;
            var trimmedComment = comment.Trim();

            var callComment = new CallComment
            {
                CallLogId = id,
                Comment = trimmedComment,
                CreatedById = currentUserId,
                CreatedDate = DateTime.Now
            };

            _context.CallComments.Add(callComment);

            _context.CallActivities.Add(new CallActivity
            {
                CallLogId = id,
                ActivityType = ActivityType.CommentAdded,
                Description = "Comment added.",
                PerformedById = currentUserId,
                PerformedDateTime = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                comment = trimmedComment,
                createdByName = _currentUser.FullName ?? _currentUser.EmployeeId,
                createdDateDisplay = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")
            });
        }

        private async Task<CallLogListViewModel> BuildListViewModelAsync(
            IQueryable<Models.Entities.CallManagement.CallLog> baseQuery,
            string? search, int? statusId, string? statusShortcut, int? priorityId, int? locationId, int? shopId, int? moduleId,
            DateTime? dateFrom, DateTime? dateTo, string sort, string dir, int page, int pageSize)
        {
            var query = baseQuery
                .Include(c => c.Location)
                .Include(c => c.Shop)
                .Include(c => c.Module)
                .Include(c => c.ProblemCategory)
                .Include(c => c.Problem)
                .Include(c => c.ReportedBy)
                .Include(c => c.AttendedBy!).ThenInclude(e => e!.ApplicationUser)
                .Include(c => c.Priority)
                .Include(c => c.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c => EF.Functions.Like(c.CallNumber, $"%{term}%") || EF.Functions.Like(c.Description, $"%{term}%"));
            }

            if (statusId.HasValue) query = query.Where(c => c.StatusId == statusId);
            if (priorityId.HasValue) query = query.Where(c => c.PriorityId == priorityId);
            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);
            if (shopId.HasValue) query = query.Where(c => c.ShopId == shopId);
            if (moduleId.HasValue) query = query.Where(c => c.ModuleId == moduleId);
            if (dateFrom.HasValue) query = query.Where(c => c.ReportedDateTime >= dateFrom.Value.Date);
            if (dateTo.HasValue) query = query.Where(c => c.ReportedDateTime < dateTo.Value.Date.AddDays(1));

            query = (sort, dir) switch
            {
                ("CallNumber", "asc") => query.OrderBy(c => c.CallNumber),
                ("CallNumber", "desc") => query.OrderByDescending(c => c.CallNumber),
                ("ReportedDateTime", "asc") => query.OrderBy(c => c.ReportedDateTime),
                ("Priority", "asc") => query.OrderBy(c => c.Priority!.Level),
                ("Priority", "desc") => query.OrderByDescending(c => c.Priority!.Level),
                ("Status", "asc") => query.OrderBy(c => c.Status!.SortOrder),
                ("Status", "desc") => query.OrderByDescending(c => c.Status!.SortOrder),
                _ => query.OrderByDescending(c => c.ReportedDateTime)
            };

            var totalCount = await query.CountAsync();
            page = Math.Max(1, page);
            pageSize = pageSize is > 0 and <= 200 ? pageSize : 25;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CallLogRowVm
                {
                    Id = c.Id,
                    CallNumber = c.CallNumber,
                    ReportedDateTime = c.ReportedDateTime,
                    LocationName = c.Location!.Name,
                    ShopName = c.Shop!.Name,
                    CallType = c.CallType,
                    ModuleName = c.Module!.Name,
                    ProblemCategoryName = c.ProblemCategory!.Name,
                    ProblemName = c.Problem != null ? c.Problem.Name : null,
                    ReportedByName = c.ReportedBy!.FullName,
                    AttendedByName = c.AttendedBy!.ApplicationUser!.FullName,
                    PriorityName = c.Priority!.Name,
                    PriorityColor = c.Priority.ColorCode,
                    StatusName = c.Status!.Name,
                    StatusCode = c.Status.Code,
                    StatusColor = c.Status.ColorCode,
                    IsLineLoss = c.IsLineLoss,
                    TimeTakenMinutes = c.TimeTakenMinutes,
                    ClosingDateTime = c.ClosingDateTime,
                    ModifiedDate = c.ModifiedDate,
                    CreatedById = c.CreatedById,
                    AttendedById = c.AttendedById,
                    HandedOverToId = c.HandedOverToId
                })
                .ToListAsync();

            return new CallLogListViewModel
            {
                Items = items,
                Search = search,
                StatusId = statusId,
                StatusShortcut = statusShortcut,
                PriorityId = priorityId,
                LocationId = locationId,
                ShopId = shopId,
                ModuleId = moduleId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Sort = sort,
                Dir = dir,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                StatusOptions = await _context.Statuses.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync(),
                PriorityOptions = await _context.Priorities.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.Level)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync(),
                LocationOptions = await _context.Locations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync(),
                ModuleOptions = await _context.Modules.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Name)
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToListAsync()
            };
        }

        [Authorize(Policy = PolicyNames.CreateCall)]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateCallLogViewModel();
            await PopulateFormAsync(model);
            return View(model);
        }

        [Authorize(Policy = PolicyNames.CreateCall)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCallLogViewModel model, string submitAction = "save")
        {
            var canOverride = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            model.CanOverrideReportedDateTime = canOverride;
            model.CanChangeAttendedBy = canOverride;

            var currentUserId = _currentUser.UserId!;
            var myEngineerId = await _context.Engineers.AsNoTracking()
                .Where(e => e.ApplicationUserId == currentUserId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            // Section 6/17 — never trust the client for these two fields unless the user
            // actually holds Admin/Support Manager; force them server-side regardless of what
            // was posted.
            if (!canOverride)
            {
                model.ReportedDateTime = DateTime.Now;
                if (myEngineerId.HasValue)
                {
                    model.AttendedById = myEngineerId.Value;
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormAsync(model);
                return View(model);
            }

            var result = await _callLogService.CreateAsync(model, currentUserId);

            if (!result.Succeeded)
            {
                foreach (var (field, message) in result.Errors)
                {
                    ModelState.AddModelError(field, message);
                }

                await PopulateFormAsync(model);
                return View(model);
            }

            var callLog = result.CallLog!;

            if (model.Attachments is { Count: > 0 })
            {
                try
                {
                    var attachments = await _attachmentService.SaveAsync(callLog.Id, model.Attachments, currentUserId);
                    if (attachments.Count > 0)
                    {
                        _context.CallAttachments.AddRange(attachments);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Attachment upload failed for CallLog {CallLogId}", callLog.Id);
                    TempData["AttachmentWarning"] = $"Call {callLog.CallNumber} was created, but the attachment could not be saved: {ex.Message}";
                }
            }

            TempData["StatusMessage"] = $"Call \"{callLog.CallNumber}\" created successfully.";

            if (submitAction == "saveAndContinue")
            {
                return RedirectToAction(nameof(Create));
            }

            // CallLog/Details doesn't exist until Phase 15 — land on Home for now.
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Policy = PolicyNames.CreateCall)]
        [HttpGet]
        public async Task<IActionResult> GetShopsByLocation(int locationId)
        {
            var shops = await _context.Shops.AsNoTracking()
                .Where(s => s.LocationId == locationId && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            return Json(shops);
        }

        [Authorize(Policy = PolicyNames.CreateCall)]
        [HttpGet]
        public async Task<IActionResult> GetProblems(int? problemCategoryId, int? moduleId)
        {
            var query = _context.Problems.AsNoTracking().Where(p => p.IsActive);

            if (problemCategoryId.HasValue)
            {
                query = query.Where(p => p.ProblemCategoryId == problemCategoryId);
            }

            if (moduleId.HasValue)
            {
                query = query.Where(p => p.ModuleId == null || p.ModuleId == moduleId);
            }

            var problems = await query
                .OrderBy(p => p.Name)
                .Select(p => new { id = p.Id, name = p.Name })
                .ToListAsync();

            return Json(problems);
        }

        private async Task PopulateFormAsync(CreateCallLogViewModel model)
        {
            var canOverride = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);
            model.CanOverrideReportedDateTime = canOverride;
            model.CanChangeAttendedBy = canOverride;
            model.CurrentUserDisplayName = _currentUser.FullName ?? _currentUser.EmployeeId ?? "Current User";

            if (model.ReportedDateTime == default)
            {
                model.ReportedDateTime = DateTime.Now;
            }

            var currentUserId = _currentUser.UserId;
            var myEngineer = currentUserId == null
                ? null
                : await _context.Engineers.AsNoTracking().FirstOrDefaultAsync(e => e.ApplicationUserId == currentUserId);

            if (model.AttendedById == 0 && myEngineer != null)
            {
                model.AttendedById = myEngineer.Id;
            }

            model.LocationOptions = await _context.Locations.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();

            model.ModuleOptions = await _context.Modules.AsNoTracking()
                .Where(m => m.IsActive).OrderBy(m => m.Name)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToListAsync();

            model.ApplicationTypeOptions = await _context.ApplicationTypes.AsNoTracking()
                .Where(a => a.IsActive).OrderBy(a => a.Name)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name }).ToListAsync();

            model.ProblemCategoryOptions = await _context.ProblemCategories.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

            model.CallCategoryOptions = await _context.CallCategories.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

            model.PriorityOptions = await _context.Priorities.AsNoTracking()
                .Where(p => p.IsActive).OrderByDescending(p => p.Level)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync();

            model.ReportedByOptions = await _context.Employees.AsNoTracking()
                .Where(e => e.IsActive).OrderBy(e => e.FullName)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.FullName + " (" + e.EmployeeCode + ")" }).ToListAsync();

            model.EngineerOptions = await _context.Engineers.AsNoTracking()
                .Where(e => e.IsActive)
                .Include(e => e.ApplicationUser)
                .OrderBy(e => e.ApplicationUser!.FullName)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName + " (" + e.ApplicationUser.EmployeeId + ")" })
                .ToListAsync();
        }

        // ------------------------------------------------------------------
        // Phase 14 — Edit Previous Entry (Sections 14-17). A dedicated
        // search-then-edit workflow, not generic CRUD: broader search filters than
        // All Calls, and per-field edit-permission tiers rather than one blanket Edit.
        // ------------------------------------------------------------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditPrevious(
            string? callNumber, DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, string? callType,
            int? moduleId, int? applicationTypeId, int? problemCategoryId, int? problemId, int? reportedById,
            int? attendedById, int? handedOverToId, int? callCategoryId, int? statusId, int? priorityId, bool? isLineLoss,
            int page = 1, int pageSize = 25, bool search = false)
        {
            var canEditAny = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);

            var vm = new EditPreviousSearchViewModel
            {
                HasSearched = search,
                CallNumber = callNumber,
                DateFrom = dateFrom,
                DateTo = dateTo,
                LocationId = locationId,
                ShopId = shopId,
                CallType = callType,
                ModuleId = moduleId,
                ApplicationTypeId = applicationTypeId,
                ProblemCategoryId = problemCategoryId,
                ProblemId = problemId,
                ReportedById = reportedById,
                AttendedById = attendedById,
                HandedOverToId = handedOverToId,
                CallCategoryId = callCategoryId,
                StatusId = statusId,
                PriorityId = priorityId,
                IsLineLoss = isLineLoss,
                Page = Math.Max(1, page),
                PageSize = pageSize is > 0 and <= 200 ? pageSize : 25,
                LocationOptions = await _context.Locations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync(),
                ModuleOptions = await _context.Modules.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Name)
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToListAsync(),
                ApplicationTypeOptions = await _context.ApplicationTypes.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.Name)
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name }).ToListAsync(),
                ProblemCategoryOptions = await _context.ProblemCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync(),
                CallCategoryOptions = await _context.CallCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync(),
                StatusOptions = await _context.Statuses.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync(),
                PriorityOptions = await _context.Priorities.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.Level)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync(),
                EngineerOptions = await _context.Engineers.AsNoTracking().Where(e => e.IsActive).Include(e => e.ApplicationUser)
                    .OrderBy(e => e.ApplicationUser!.FullName)
                    .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName }).ToListAsync()
            };

            if (!search)
            {
                return View(vm);
            }

            var query = _context.CallLogs.AsNoTracking()
                .Include(c => c.Location).Include(c => c.Shop).Include(c => c.Module)
                .Include(c => c.ProblemCategory).Include(c => c.Problem).Include(c => c.ReportedBy)
                .Include(c => c.AttendedBy!).ThenInclude(e => e!.ApplicationUser)
                .Include(c => c.Priority).Include(c => c.Status)
                .AsQueryable();

            // Section 7 — a Support Engineer may only reach their own assigned calls through
            // this screen; Admin/Support Manager can search everything.
            if (!canEditAny)
            {
                var currentUserId = _currentUser.UserId!;
                var myEngineerId = await _context.Engineers.AsNoTracking()
                    .Where(e => e.ApplicationUserId == currentUserId).Select(e => (int?)e.Id).FirstOrDefaultAsync();
                query = query.Where(c => myEngineerId != null && c.AttendedById == myEngineerId);
            }

            if (!string.IsNullOrWhiteSpace(callNumber)) query = query.Where(c => EF.Functions.Like(c.CallNumber, $"%{callNumber.Trim()}%"));
            if (dateFrom.HasValue) query = query.Where(c => c.ReportedDateTime >= dateFrom.Value.Date);
            if (dateTo.HasValue) query = query.Where(c => c.ReportedDateTime < dateTo.Value.Date.AddDays(1));
            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);
            if (shopId.HasValue) query = query.Where(c => c.ShopId == shopId);
            if (!string.IsNullOrWhiteSpace(callType) && Enum.TryParse<Models.Enums.CallType>(callType, out var ct)) query = query.Where(c => c.CallType == ct);
            if (moduleId.HasValue) query = query.Where(c => c.ModuleId == moduleId);
            if (applicationTypeId.HasValue) query = query.Where(c => c.ApplicationTypeId == applicationTypeId);
            if (problemCategoryId.HasValue) query = query.Where(c => c.ProblemCategoryId == problemCategoryId);
            if (problemId.HasValue) query = query.Where(c => c.ProblemId == problemId);
            if (reportedById.HasValue) query = query.Where(c => c.ReportedById == reportedById);
            if (attendedById.HasValue) query = query.Where(c => c.AttendedById == attendedById);
            if (handedOverToId.HasValue) query = query.Where(c => c.HandedOverToId == handedOverToId);
            if (callCategoryId.HasValue) query = query.Where(c => c.CallCategoryId == callCategoryId);
            if (statusId.HasValue) query = query.Where(c => c.StatusId == statusId);
            if (priorityId.HasValue) query = query.Where(c => c.PriorityId == priorityId);
            if (isLineLoss.HasValue) query = query.Where(c => c.IsLineLoss == isLineLoss);

            query = query.OrderByDescending(c => c.ReportedDateTime);

            vm.TotalCount = await query.CountAsync();

            vm.Items = await query
                .Skip((vm.Page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .Select(c => new CallLogRowVm
                {
                    Id = c.Id,
                    CallNumber = c.CallNumber,
                    ReportedDateTime = c.ReportedDateTime,
                    LocationName = c.Location!.Name,
                    ShopName = c.Shop!.Name,
                    CallType = c.CallType,
                    ModuleName = c.Module!.Name,
                    ProblemCategoryName = c.ProblemCategory!.Name,
                    ProblemName = c.Problem != null ? c.Problem.Name : null,
                    ReportedByName = c.ReportedBy!.FullName,
                    AttendedByName = c.AttendedBy!.ApplicationUser!.FullName,
                    PriorityName = c.Priority!.Name,
                    PriorityColor = c.Priority.ColorCode,
                    StatusName = c.Status!.Name,
                    StatusCode = c.Status.Code,
                    StatusColor = c.Status.ColorCode,
                    IsLineLoss = c.IsLineLoss,
                    TimeTakenMinutes = c.TimeTakenMinutes,
                    ClosingDateTime = c.ClosingDateTime,
                    ModifiedDate = c.ModifiedDate
                })
                .ToListAsync();

            return View(vm);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditPreviousEntry(int id)
        {
            var callLog = await _callLogService.GetByIdAsync(id);
            if (callLog == null)
            {
                return NotFound();
            }

            var (canEditSpecial, canEditNormal, isClosed) = await GetEditPermissionsAsync(callLog);
            if (!canEditNormal)
            {
                return Forbid();
            }

            var model = MapToEditViewModel(callLog, canEditSpecial, canEditNormal, isClosed);
            await PopulateEditFormOptionsAsync(model);

            if (TempData["RecentChanges"] is string changesJson && !string.IsNullOrEmpty(changesJson))
            {
                model.RecentChanges = System.Text.Json.JsonSerializer.Deserialize<List<string>>(changesJson) ?? new();
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPreviousEntry(int id, EditCallLogViewModel model)
        {
            var callLog = await _callLogService.GetByIdAsync(id);
            if (callLog == null)
            {
                return NotFound();
            }

            var (canEditSpecial, canEditNormal, isClosed) = await GetEditPermissionsAsync(callLog);
            if (!canEditNormal)
            {
                return Forbid();
            }

            model.CanEditSpecialFields = canEditSpecial;
            model.CanEditNormalFields = canEditNormal;
            model.IsClosed = isClosed;

            if (!ModelState.IsValid)
            {
                await PopulateEditFormOptionsAsync(model);
                return View(model);
            }

            var currentUserId = _currentUser.UserId!;
            var result = await _callLogService.UpdateAsync(id, model, currentUserId, canEditSpecial);

            if (!result.Succeeded)
            {
                foreach (var (field, message) in result.Errors)
                {
                    ModelState.AddModelError(field, message);
                }

                await PopulateEditFormOptionsAsync(model);
                return View(model);
            }

            TempData["StatusMessage"] = result.Changes.Count == 0
                ? "No changes were made."
                : $"Call \"{callLog.CallNumber}\" updated successfully. {result.Changes.Count} field(s) changed.";
            TempData["RecentChanges"] = System.Text.Json.JsonSerializer.Serialize(result.Changes);

            return RedirectToAction(nameof(EditPreviousEntry), new { id });
        }

        private async Task<(bool CanEditSpecial, bool CanEditNormal, bool IsClosed)> GetEditPermissionsAsync(Models.Entities.CallManagement.CallLog callLog)
        {
            var canEditAny = _currentUser.IsInRole(RoleNames.Admin) || _currentUser.IsInRole(RoleNames.SupportManager);

            var closedStatus = await _context.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.Code == CallStatusCodes.Closed);
            var isClosed = closedStatus != null && callLog.StatusId == closedStatus.Id;

            if (canEditAny)
            {
                // Admin / Support Manager: full access, including closed calls.
                return (true, true, isClosed);
            }

            if (isClosed)
            {
                // Section 17 — closed calls are otherwise read-only.
                return (false, false, true);
            }

            // Support Engineer: normal-tier fields only, and only on their own assigned call.
            var currentUserId = _currentUser.UserId;
            var myEngineerId = currentUserId == null
                ? null
                : await _context.Engineers.AsNoTracking().Where(e => e.ApplicationUserId == currentUserId).Select(e => (int?)e.Id).FirstOrDefaultAsync();

            var isMyCall = myEngineerId != null && callLog.AttendedById == myEngineerId;
            return (false, isMyCall, false);
        }

        private static EditCallLogViewModel MapToEditViewModel(Models.Entities.CallManagement.CallLog callLog, bool canEditSpecial, bool canEditNormal, bool isClosed)
        {
            return new EditCallLogViewModel
            {
                Id = callLog.Id,
                CallNumber = callLog.CallNumber,
                CreatedByName = callLog.CreatedBy?.FullName ?? "-",
                CreatedDate = callLog.CreatedDate,
                StatusName = callLog.Status?.Name ?? "-",
                StatusColor = callLog.Status?.ColorCode ?? "#6c757d",
                TimeTakenDisplay = callLog.TimeTakenMinutes.HasValue
                    ? $"{callLog.TimeTakenMinutes / 60 / 24}d {callLog.TimeTakenMinutes / 60 % 24}h {callLog.TimeTakenMinutes % 60}m"
                    : "-",
                LocationId = callLog.LocationId,
                ShopId = callLog.ShopId,
                ModuleId = callLog.ModuleId,
                ApplicationTypeId = callLog.ApplicationTypeId,
                ReportedById = callLog.ReportedById,
                ReportedDateTime = callLog.ReportedDateTime,
                AttendedById = callLog.AttendedById,
                IsLineLoss = callLog.IsLineLoss,
                LineLossArea = callLog.LineLossArea,
                LineLossStartDateTime = callLog.LineLossStartDateTime,
                LineLossEndDateTime = callLog.LineLossEndDateTime,
                LineLossAffectedVehicles = callLog.LineLossAffectedVehicles,
                ProblemCategoryId = callLog.ProblemCategoryId,
                ProblemId = callLog.ProblemId,
                Description = callLog.Description,
                CallCategoryId = callLog.CallCategoryId,
                PriorityId = callLog.PriorityId,
                ICAPCA = callLog.ICAPCA,
                Remarks = callLog.Remarks,
                CanEditSpecialFields = canEditSpecial,
                CanEditNormalFields = canEditNormal,
                IsClosed = isClosed
            };
        }

        private async Task PopulateEditFormOptionsAsync(EditCallLogViewModel model)
        {
            model.LocationOptions = await _context.Locations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();

            model.ShopOptions = await _context.Shops.AsNoTracking().Where(s => s.IsActive && s.LocationId == model.LocationId).OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync();

            model.ModuleOptions = await _context.Modules.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Name)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToListAsync();

            model.ApplicationTypeOptions = await _context.ApplicationTypes.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.Name)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name }).ToListAsync();

            model.ProblemCategoryOptions = await _context.ProblemCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

            model.ProblemOptions = await _context.Problems.AsNoTracking().Where(p => p.IsActive && p.ProblemCategoryId == model.ProblemCategoryId).OrderBy(p => p.Name)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync();

            model.CallCategoryOptions = await _context.CallCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

            model.PriorityOptions = await _context.Priorities.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.Level)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync();

            model.ReportedByOptions = await _context.Employees.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.FullName)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.FullName + " (" + e.EmployeeCode + ")" }).ToListAsync();

            model.EngineerOptions = await _context.Engineers.AsNoTracking().Where(e => e.IsActive).Include(e => e.ApplicationUser)
                .OrderBy(e => e.ApplicationUser!.FullName)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName + " (" + e.ApplicationUser.EmployeeId + ")" })
                .ToListAsync();
        }
    }
}
