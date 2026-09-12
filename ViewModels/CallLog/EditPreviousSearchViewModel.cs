using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    // Section 15 — the fuller filter set for Edit Previous Entry's search step (wider than the
    // All Calls list filters in Section 12).
    public class EditPreviousSearchViewModel
    {
        public List<CallLogRowVm> Items { get; set; } = new();
        public bool HasSearched { get; set; }

        public string? CallNumber { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LocationId { get; set; }
        public int? ShopId { get; set; }
        public string? CallType { get; set; }
        public int? ModuleId { get; set; }
        public int? ApplicationTypeId { get; set; }
        public int? ProblemCategoryId { get; set; }
        public int? ProblemId { get; set; }
        public int? ReportedById { get; set; }
        public int? AttendedById { get; set; }
        public int? HandedOverToId { get; set; }
        public int? CallCategoryId { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public bool? IsLineLoss { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
        public List<SelectListItem> ApplicationTypeOptions { get; set; } = new();
        public List<SelectListItem> ProblemCategoryOptions { get; set; } = new();
        public List<SelectListItem> CallCategoryOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
        public List<SelectListItem> EngineerOptions { get; set; } = new();
    }
}
