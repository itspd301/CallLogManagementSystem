using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.ViewModels.Masters
{
    public class SLAConfigurationFormViewModel
    {
        public int PriorityId { get; set; }
        public string PriorityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Response time is required.")]
        [Range(1, 100000, ErrorMessage = "Enter a positive number of minutes.")]
        [Display(Name = "Response Time (minutes)")]
        public int ResponseMinutes { get; set; }

        [Required(ErrorMessage = "Resolution time is required.")]
        [Range(1, 100000, ErrorMessage = "Enter a positive number of minutes.")]
        [Display(Name = "Resolution Time (minutes)")]
        public int ResolutionMinutes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
