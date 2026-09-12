using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class EmployeeFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee Code is required.")]
        [MaxLength(20)]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Designation { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        public int LocationId { get; set; }

        [Display(Name = "Shop")]
        public int? ShopId { get; set; }

        [MaxLength(20)]
        [Display(Name = "Contact Number")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string? ContactNumber { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEdit { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ShopOptions { get; set; } = new();
    }
}
