using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Reports
{
    public enum ReportGroupBy
    {
        Day,
        Month,
        Location,
        Shop,
        Module,
        ProblemCategory,
        Status
    }

    public class ReportSummaryRowVm
    {
        public string GroupLabel { get; set; } = string.Empty;
        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int ClosedCount { get; set; }
        public int LineLossCount { get; set; }
        public double? AverageResolutionMinutes { get; set; }
    }

    public class CallReportViewModel
    {
        public List<ReportSummaryRowVm> Rows { get; set; } = new();

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LocationId { get; set; }
        public int? ShopId { get; set; }
        public int? ModuleId { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public ReportGroupBy GroupBy { get; set; } = ReportGroupBy.Day;

        public int GrandTotal { get; set; }
        public int GrandOpen { get; set; }
        public int GrandClosed { get; set; }
        public int GrandLineLoss { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
    }
}
