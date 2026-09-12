using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    // Section 21 — full handover history; CallLog.HandedOverToId always reflects the
    // most recent row here, but every prior handover stays for traceability.
    public class CallHandover
    {
        public int Id { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        public int FromEngineerId { get; set; }
        [ForeignKey(nameof(FromEngineerId))]
        public Engineer? FromEngineer { get; set; }

        public int ToEngineerId { get; set; }
        [ForeignKey(nameof(ToEngineerId))]
        public Engineer? ToEngineer { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public DateTime HandoverDateTime { get; set; } = DateTime.Now;

        [Required]
        public string PerformedById { get; set; } = string.Empty;
        [ForeignKey(nameof(PerformedById))]
        public ApplicationUser? PerformedBy { get; set; }
    }
}
