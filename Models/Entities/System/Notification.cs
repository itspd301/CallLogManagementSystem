using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Models.Entities.System
{
    // In-app bell notifications: Assigned to me / Handed over to me / Reopened. Polled by the
    // header dropdown every ~30s rather than pushed, so no SignalR/hub is involved.
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        public NotificationType Type { get; set; }

        [Required, MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
