using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Reports
{
    public class SlaSummaryRowVm
    {
        public string PriorityName { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }
        public int Total { get; set; }
        public int Met { get; set; }
        public int Breached { get; set; }
        public int Pending { get; set; }
        public double BreachPercentage => Total == 0 ? 0 : Math.Round(Breached * 100.0 / Total, 1);
    }

    public class SlaDetailRowVm
    {
        public string CallNumber { get; set; } = string.Empty;
        public int CallLogId { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime ReportedDateTime { get; set; }
        public int? ResolutionTargetMinutes { get; set; }
        public int ActualMinutes { get; set; }
        public string SlaStatus { get; set; } = string.Empty; // Met / Breached / Pending / Not Configured
    }

    public class SlaReportViewModel
    {
        public List<SlaSummaryRowVm> Summary { get; set; } = new();
        public List<SlaDetailRowVm> Details { get; set; } = new();

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? PriorityId { get; set; }
        public int? LocationId { get; set; }
        public bool BreachedOnly { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
    }
}
