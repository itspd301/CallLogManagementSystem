using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    // Section 11 — the central entity. Every field here is deliberate: it maps 1:1 to a
    // requirement in Sections 9-11. Nothing is trimmed for convenience.
    public class CallLog
    {
        public int Id { get; set; }

        // System-generated (Phase 10: CALL-YYYY-NNNNNN). Never editable after creation.
        [Required, MaxLength(30)]
        public string CallNumber { get; set; } = string.Empty;

        public int LocationId { get; set; }
        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }

        public int ShopId { get; set; }
        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        public CallType CallType { get; set; }

        public int ModuleId { get; set; }
        [ForeignKey(nameof(ModuleId))]
        public Module? Module { get; set; }

        public int? ApplicationTypeId { get; set; }
        [ForeignKey(nameof(ApplicationTypeId))]
        public ApplicationType? ApplicationType { get; set; }

        public int ProblemCategoryId { get; set; }
        [ForeignKey(nameof(ProblemCategoryId))]
        public ProblemCategory? ProblemCategory { get; set; }

        public int? ProblemId { get; set; }
        [ForeignKey(nameof(ProblemId))]
        public Problem? Problem { get; set; }

        // "Problem Reported By" — plant personnel (Employee master), not a system user.
        public int ReportedById { get; set; }
        [ForeignKey(nameof(ReportedById))]
        public Employee? ReportedBy { get; set; }

        [Required]
        public DateTime ReportedDateTime { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public int CallCategoryId { get; set; }
        [ForeignKey(nameof(CallCategoryId))]
        public CallCategory? CallCategory { get; set; }

        public int PriorityId { get; set; }
        [ForeignKey(nameof(PriorityId))]
        public Priority? Priority { get; set; }

        public int StatusId { get; set; }
        [ForeignKey(nameof(StatusId))]
        public Status? Status { get; set; }

        public string? ICAPCA { get; set; }

        // Attended By / Handed Over To reference the Engineer master (which itself links to
        // ApplicationUser), keeping engineer-facing dropdowns off the raw Identity table.
        public int AttendedById { get; set; }
        [ForeignKey(nameof(AttendedById))]
        public Engineer? AttendedBy { get; set; }

        public int? HandedOverToId { get; set; }
        [ForeignKey(nameof(HandedOverToId))]
        public Engineer? HandedOverTo { get; set; }

        public bool IsLineLoss { get; set; }

        [MaxLength(150)]
        public string? LineLossArea { get; set; }

        public DateTime? LineLossStartDateTime { get; set; }
        public DateTime? LineLossEndDateTime { get; set; }
        public int? LineLossDurationMinutes { get; set; }
        public int? LineLossAffectedVehicles { get; set; }

        public DateTime? ClosingDateTime { get; set; }

        // Recomputed server-side whenever ClosingDateTime changes: ClosingDateTime - ReportedDateTime.
        public int? TimeTakenMinutes { get; set; }

        public string? Remarks { get; set; }

        // System-controlled (Section 17): set once at creation, never reassigned.
        [Required]
        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey(nameof(CreatedById))]
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModifiedById { get; set; }
        [ForeignKey(nameof(ModifiedById))]
        public ApplicationUser? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<CallActivity> Activities { get; set; } = new List<CallActivity>();
        public ICollection<CallComment> Comments { get; set; } = new List<CallComment>();
        public ICollection<CallAttachment> Attachments { get; set; } = new List<CallAttachment>();
        public ICollection<CallHandover> Handovers { get; set; } = new List<CallHandover>();
        public ICollection<CallAssignment> Assignments { get; set; } = new List<CallAssignment>();
    }
}
