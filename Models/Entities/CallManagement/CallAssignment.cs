using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    // Section 20 — full assignment history (a call can be reassigned multiple times).
    public class CallAssignment
    {
        public int Id { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        public int EngineerId { get; set; }
        [ForeignKey(nameof(EngineerId))]
        public Engineer? Engineer { get; set; }

        [Required]
        public string AssignedById { get; set; } = string.Empty;
        [ForeignKey(nameof(AssignedById))]
        public ApplicationUser? AssignedBy { get; set; }

        public DateTime AssignedDateTime { get; set; } = DateTime.Now;

        public string? Remarks { get; set; }
    }
}
