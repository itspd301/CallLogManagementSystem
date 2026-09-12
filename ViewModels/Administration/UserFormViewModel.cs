using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Administration
{
    public class UserFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        [MaxLength(20)]
        [Display(Name = "User ID / Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Designation { get; set; }

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Initial Password")]
        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEdit { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class ResetPasswordAdminViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
