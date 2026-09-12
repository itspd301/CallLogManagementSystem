using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class AssignEngineerViewModel
    {
        public int CallLogId { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public string CurrentAttendedByName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select an engineer to assign.")]
        [Display(Name = "Assign To")]
        public int EngineerId { get; set; }

        public string? Remarks { get; set; }

        public List<SelectListItem> EngineerOptions { get; set; } = new();
    }

    public class AssignmentHistoryVm
    {
        public string EngineerName { get; set; } = string.Empty;
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime AssignedDateTime { get; set; }
        public string? Remarks { get; set; }
    }
}
