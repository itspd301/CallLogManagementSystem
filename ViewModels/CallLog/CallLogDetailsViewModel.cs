using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class CallLogDetailsViewModel
    {
        public int Id { get; set; }
        public string CallNumber { get; set; } = string.Empty;

        public string PriorityName { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? StatusColor { get; set; }

        public string StatusCode { get; set; } = string.Empty;

        public string LocationName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public CallType CallType { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? ApplicationTypeName { get; set; }

        public string ProblemCategoryName { get; set; } = string.Empty;
        public string? ProblemName { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public DateTime ReportedDateTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CallCategoryName { get; set; } = string.Empty;

        public string AttendedByName { get; set; } = string.Empty;
        public string? HandedOverToName { get; set; }
        public string? ICAPCA { get; set; }
        public string? Remarks { get; set; }

        public bool IsLineLoss { get; set; }
        public string? LineLossArea { get; set; }
        public DateTime? LineLossStartDateTime { get; set; }
        public DateTime? LineLossEndDateTime { get; set; }
        public int? LineLossDurationMinutes { get; set; }
        public int? LineLossAffectedVehicles { get; set; }

        public DateTime? ClosingDateTime { get; set; }
        public int? TimeTakenMinutes { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public List<AttachmentVm> Attachments { get; set; } = new();
        public List<CommentVm> Comments { get; set; } = new();
        public List<ActivityVm> Activities { get; set; } = new();
        public List<AssignmentHistoryVm> AssignmentHistory { get; set; } = new();
        public List<HandoverHistoryVm> HandoverHistory { get; set; } = new();

        // Permissions — drive which action buttons render.
        public bool CanEdit { get; set; }
        public bool CanAddCommentOrAttachment { get; set; }
        public bool CanAssign { get; set; }
        public bool CanHandover { get; set; }
        public bool CanStartProgress { get; set; }
        public bool CanResolve { get; set; }
        public bool CanClose { get; set; }
        public bool CanReopen { get; set; }
        public bool CanResolveAndClose { get; set; }

        public AssignEngineerViewModel AssignForm { get; set; } = new();
        public HandoverViewModel HandoverForm { get; set; } = new();
        public ResolveCallViewModel ResolveForm { get; set; } = new();
        public ResolveCallViewModel ResolveAndCloseForm { get; set; } = new();
    }

    public class ResolveCallViewModel
    {
        public string? ICAPCA { get; set; }
        public string? Remarks { get; set; }
    }

    public class AttachmentVm
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CommentVm
    {
        public string Comment { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class ActivityVm
    {
        public string TypeLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedDateTime { get; set; }
    }
}
