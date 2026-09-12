using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class HandoverViewModel
    {
        public int CallLogId { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public string FromEngineerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select the engineer to hand this call over to.")]
        [Display(Name = "To Engineer")]
        public int ToEngineerId { get; set; }

        [Required(ErrorMessage = "A reason is required for handover.")]
        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public List<SelectListItem> EngineerOptions { get; set; } = new();
    }

    public class HandoverHistoryVm
    {
        public string FromEngineerName { get; set; } = string.Empty;
        public string ToEngineerName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime HandoverDateTime { get; set; }
    }
}
