using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class CallLogListViewModel
    {
        public List<CallLogRowVm> Items { get; set; } = new();

        // Filters
        public string? Search { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? LocationId { get; set; }
        public int? ShopId { get; set; }
        public int? ModuleId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // The sidebar's Open/Closed/Reopened shortcuts arrive as this string and get resolved
        // to StatusId server-side; kept here only so the view can echo it back into pagination links.
        public string? StatusShortcut { get; set; }

        public string PageTitle { get; set; } = "All Calls";
        public bool ShowRelationColumn { get; set; }

        public string Sort { get; set; } = "ReportedDateTime";
        public string Dir { get; set; } = "desc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
    }
}
