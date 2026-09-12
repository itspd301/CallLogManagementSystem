using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    public class CallComment
    {
        public int Id { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey(nameof(CreatedById))]
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
