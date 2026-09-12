using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class SimpleMasterFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public bool HasCode { get; set; }
        public string EntityDisplayName { get; set; } = string.Empty;
        public string ControllerName { get; set; } = string.Empty;
        public bool IsEdit { get; set; }
    }
}
