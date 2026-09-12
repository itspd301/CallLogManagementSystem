using CallLogManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Audit
{
    public class AuditLogRowVm
    {
        public long Id { get; set; }
        public string? UserName { get; set; }
        public string? EmployeeId { get; set; }
        public AuditAction Action { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IPAddress { get; set; }
    }

    public class AuditLogListViewModel
    {
        public List<AuditLogRowVm> Items { get; set; } = new();

        public string? Search { get; set; }
        public AuditAction? Action { get; set; }
        public string? EntityName { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<SelectListItem> EntityNameOptions { get; set; } = new();
    }
}
