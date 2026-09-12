using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User ID / Employee ID is required.")]
        [Display(Name = "User ID / Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
