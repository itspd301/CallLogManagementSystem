using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Dashboard
{
    public class ChartDataVm
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Data { get; set; } = new();
    }

    public class DashboardViewModel
    {
        // Section 26 KPI cards
        public int TotalCalls { get; set; }
        public int OpenCount { get; set; }
        public int AssignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int OnHoldCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int ReopenedCount { get; set; }
        public int CriticalCount { get; set; }
        public int LineLossCount { get; set; }
        public int SlaBreachedCount { get; set; }
        public double? AverageResolutionMinutes { get; set; }

        // Section 26 charts
        public ChartDataVm CallsByDay { get; set; } = new();
        public ChartDataVm CallsByLocation { get; set; } = new();
        public ChartDataVm CallsByShop { get; set; } = new();
        public ChartDataVm CallsByModule { get; set; } = new();
        public ChartDataVm CallsByProblemCategory { get; set; } = new();
        public ChartDataVm CallsByEngineer { get; set; } = new();
        public ChartDataVm OpenVsClosed { get; set; } = new();
        public ChartDataVm CallsByPriority { get; set; } = new();
        public ChartDataVm CallsByCallType { get; set; } = new();
        public ChartDataVm LineLossByShop { get; set; } = new();
        public ChartDataVm ResolutionTimeByPriority { get; set; } = new();

        // Filters
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LocationId { get; set; }
        public int? ShopId { get; set; }
        public int? ModuleId { get; set; }
        public int? EngineerId { get; set; }
        public int? PriorityId { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
        public List<SelectListItem> EngineerOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
    }
}
