using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class CallLogRowVm
    {
        public int Id { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public DateTime ReportedDateTime { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public CallType CallType { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ProblemCategoryName { get; set; } = string.Empty;
        public string? ProblemName { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string AttendedByName { get; set; } = string.Empty;
        public string PriorityName { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string? StatusColor { get; set; }
        public bool IsLineLoss { get; set; }
        public int? TimeTakenMinutes { get; set; }
        public DateTime? ClosingDateTime { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Raw FKs — cheap to project alongside everything else, and let My Calls compute
        // RelationTags client-side (in memory) instead of an extra query per row.
        public string CreatedById { get; set; } = string.Empty;
        public int AttendedById { get; set; }
        public int? HandedOverToId { get; set; }

        // Populated only on My Calls — why this call appears in the current user's list
        // (Created / Attended / Handed Over / Assigned), since a call can qualify via more
        // than one relationship at once.
        public List<string> RelationTags { get; set; } = new();
    }
}
