using System.ComponentModel.DataAnnotations;
using CallLogManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CallLogManagementSystem.ViewModels.CallLog
{
    // Section 14/17 — the dedicated Edit Previous Entry form. Unlike Create, every field here
    // carries its own edit-permission tier (System-controlled / Special-permission / Normal),
    // enforced both by disabling inputs in the view and re-checked server-side on POST.
    public class EditCallLogViewModel
    {
        public int Id { get; set; }

        // System-controlled — always read-only display.
        public string CallNumber { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public string? TimeTakenDisplay { get; set; }

        // Special permission required (Admin / Support Manager only)
        [Required(ErrorMessage = "Location is required.")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Shop is required.")]
        public int ShopId { get; set; }

        [Required(ErrorMessage = "Module is required.")]
        [Display(Name = "Module")]
        public int ModuleId { get; set; }

        [Display(Name = "Application Type")]
        public int? ApplicationTypeId { get; set; }

        [Required(ErrorMessage = "Problem Reported By is required.")]
        [Display(Name = "Problem Reported By")]
        public int ReportedById { get; set; }

        [Required(ErrorMessage = "Reported Date & Time is required.")]
        [Display(Name = "Reported Date & Time")]
        public DateTime ReportedDateTime { get; set; }

        [Required(ErrorMessage = "Attended By is required.")]
        [Display(Name = "Attended By")]
        public int AttendedById { get; set; }

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

        // Normally editable (Admin / Support Manager / the attending Support Engineer)
        [Required(ErrorMessage = "Problem Category is required.")]
        [Display(Name = "Problem Category")]
        public int ProblemCategoryId { get; set; }

        [Display(Name = "Problem")]
        public int? ProblemId { get; set; }

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

        public string? Remarks { get; set; }

        // Permission flags — drive what the view renders as editable vs read-only.
        // Never trust these alone: the controller/service re-derives and enforces them on POST.
        public bool CanEditSpecialFields { get; set; }
        public bool CanEditNormalFields { get; set; }
        public bool IsReadOnly => !CanEditNormalFields;
        public bool IsClosed { get; set; }

        public List<SelectListItem> LocationOptions { get; set; } = new();
        public List<SelectListItem> ShopOptions { get; set; } = new();
        public List<SelectListItem> ModuleOptions { get; set; } = new();
        public List<SelectListItem> ApplicationTypeOptions { get; set; } = new();
        public List<SelectListItem> ProblemCategoryOptions { get; set; } = new();
        public List<SelectListItem> ProblemOptions { get; set; } = new();
        public List<SelectListItem> CallCategoryOptions { get; set; } = new();
        public List<SelectListItem> PriorityOptions { get; set; } = new();
        public List<SelectListItem> ReportedByOptions { get; set; } = new();
        public List<SelectListItem> EngineerOptions { get; set; } = new();

        // Populated via TempData after a successful save so the page can show exactly what changed.
        public List<string> RecentChanges { get; set; } = new();
    }
}
