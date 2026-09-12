using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Reports
{
    public class LineLossRowVm
    {
        public int CallLogId { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string? LineLossArea { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int? DurationMinutes { get; set; }
        public int? AffectedVehicles { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class LineLossByShopVm
    {
        public string ShopName { get; set; } = string.Empty;
        public int IncidentCount { get; set; }
        public int TotalDowntimeMinutes { get; set; }
        public int TotalAffectedVehicles { get; set; }
    }

    public class LineLossReportViewModel
    {
        public List<LineLossRowVm> Rows { get; set; } = new();
        public List<LineLossByShopVm> ByShop { get; set; } = new();

        public int TotalIncidents { get; set; }
        public int TotalDowntimeMinutes { get; set; }
        public int TotalAffectedVehicles { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LocationId { get; set; }
        public int? ShopId { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ShopOptions { get; set; } = new();
    }
}
