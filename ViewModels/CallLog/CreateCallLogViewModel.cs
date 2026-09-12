using System.ComponentModel.DataAnnotations;
using CallLogManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    public class CreateCallLogViewModel
    {
        [Required(ErrorMessage = "Location is required.")]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Shop is required.")]
        [Display(Name = "Shop")]
        public int ShopId { get; set; }

        [Required(ErrorMessage = "Call Type is required.")]
        [Display(Name = "Call Type")]
        public CallType CallType { get; set; } = CallType.UserSupport;

        [Required(ErrorMessage = "Module is required.")]
        [Display(Name = "Module")]
        public int ModuleId { get; set; }

        [Display(Name = "Application Type")]
        public int? ApplicationTypeId { get; set; }

        [Required(ErrorMessage = "Problem Category is required.")]
        [Display(Name = "Problem Category")]
        public int ProblemCategoryId { get; set; }

        [Display(Name = "Problem")]
        public int? ProblemId { get; set; }

        [Required(ErrorMessage = "Problem Reported By is required.")]
        [Display(Name = "Problem Reported By")]
        public int ReportedById { get; set; }

        [Required(ErrorMessage = "Reported Date & Time is required.")]
        [Display(Name = "Reported Date & Time")]
        public DateTime ReportedDateTime { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description & Cause of Problem")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Call Category is required.")]
        [Display(Name = "Call Category")]
        public int CallCategoryId { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public int PriorityId { get; set; }

        [Display(Name = "ICA / PCA")]
        public string? ICAPCA { get; set; }

        [Required(ErrorMessage = "Attended By is required.")]
        [Display(Name = "Attended By")]
        public int AttendedById { get; set; }

        [Display(Name = "Handed Over To")]
        public int? HandedOverToId { get; set; }

        [Display(Name = "Is Line Loss")]
        public bool IsLineLoss { get; set; }

        [Display(Name = "Line / Area")]
        public string? LineLossArea { get; set; }

        [Display(Name = "Downtime Start")]
        public DateTime? LineLossStartDateTime { get; set; }

        [Display(Name = "Downtime End")]
        public DateTime? LineLossEndDateTime { get; set; }

        [Display(Name = "Affected Vehicles")]
        [Range(1, int.MaxValue, ErrorMessage = "Affected Vehicles must be at least 1.")]
        public int? LineLossAffectedVehicles { get; set; }

        public string? Remarks { get; set; }

        public List<IFormFile>? Attachments { get; set; }

        // Whether the current user is allowed to override ReportedDateTime / AttendedBy away
        // from their defaults — enforced again server-side in the controller, this only drives
        // whether the fields render editable.
        public bool CanOverrideReportedDateTime { get; set; }
        public bool CanChangeAttendedBy { get; set; }

        public string CurrentUserDisplayName { get; set; } = string.Empty;

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
        public List<SelectListItem> ApplicationTypeOptions { get; set; } = new();
        public List<SelectListItem> ProblemCategoryOptions { get; set; } = new();
        public List<SelectListItem> CallCategoryOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
        public List<SelectListItem> ReportedByOptions { get; set; } = new();
        public List<SelectListItem> EngineerOptions { get; set; } = new();
    }
}
