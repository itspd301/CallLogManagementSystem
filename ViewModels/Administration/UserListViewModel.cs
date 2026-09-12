using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Administration
{
    public class UserRowVm
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? LocationName { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserRowVm> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<SelectListItem> RoleOptions { get; set; } = new();
    }
}
