using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class EngineerFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "User is required.")]
        [Display(Name = "System User")]
        public string ApplicationUserId { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Specialization { get; set; }

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEdit { get; set; }

        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> LocationOptions { get; set; } = new();
    }
}
