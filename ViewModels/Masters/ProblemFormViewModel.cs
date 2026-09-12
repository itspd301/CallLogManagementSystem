using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class ProblemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Problem Category is required.")]
        [Display(Name = "Problem Category")]
        public int ProblemCategoryId { get; set; }

        [Display(Name = "Module")]
        public int? ModuleId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEdit { get; set; }

        public List<SelectListItem> ProblemCategoryOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
    }
}
