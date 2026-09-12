using Microsoft.AspNetCore.Identity;

namespace CallLogManagementSystem.Models.Entities.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
    }
}
