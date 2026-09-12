using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    // Section 28 — one active configuration per Priority (Critical/High/Medium/Low).
    public class SLAConfiguration : MasterEntity
    {
        public int PriorityId { get; set; }

        [ForeignKey(nameof(PriorityId))]
        public Priority? Priority { get; set; }

        [Required]
        public int ResponseMinutes { get; set; }

        [Required]
        public int ResolutionMinutes { get; set; }
    }
}
