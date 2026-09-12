using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class ShopFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEdit { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
    }
}
