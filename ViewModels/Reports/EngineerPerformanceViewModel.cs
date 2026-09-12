using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Reports
{
    // Section 27
    public class EngineerPerformanceRowVm
    {
        public string EngineerName { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int ReopenedCount { get; set; }
        public double? AverageResolutionMinutes { get; set; }
        public int CriticalCount { get; set; }
        public int LineLossCount { get; set; }
        public int SlaBreachCount { get; set; }
    }

    public class EngineerPerformanceViewModel
    {
        public List<EngineerPerformanceRowVm> Rows { get; set; } = new();

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LocationId { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
    }
}
