using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    // Section 19 — the append-only Activity Timeline. Written automatically by the services
    // layer (never edited/deleted by users) whenever a tracked field changes.
    public class CallActivity
    {
        public int Id { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        public ActivityType ActivityType { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public int? OldStatusId { get; set; }
        [ForeignKey(nameof(OldStatusId))]
        public Status? OldStatus { get; set; }

        public int? NewStatusId { get; set; }
        [ForeignKey(nameof(NewStatusId))]
        public Status? NewStatus { get; set; }

        [Required]
        public string PerformedById { get; set; } = string.Empty;
        [ForeignKey(nameof(PerformedById))]
        public ApplicationUser? PerformedBy { get; set; }

        public DateTime PerformedDateTime { get; set; } = DateTime.Now;

        public string? Remarks { get; set; }
    }
}
