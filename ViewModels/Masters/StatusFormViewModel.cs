using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class StatusFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(30)]
        [Display(Name = "Code (used by workflow logic — change with care)")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Color")]
        public string? ColorCode { get; set; } = "#0d6efd";

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsEdit { get; set; }
    }
}
