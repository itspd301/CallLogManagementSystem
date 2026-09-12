using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Models.Entities.System
{
    // Section 29 — system-wide security/audit trail. Independent of CallActivity: this table
    // also records logins, master data edits and user/role changes that fall outside any call.
    public class AuditLog
    {
        public long Id { get; set; }

        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public AuditAction Action { get; set; }

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [MaxLength(45)]
        public string? IPAddress { get; set; }
    }
}
