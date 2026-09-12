using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    public class Priority : MasterEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Stable key from Constants.PriorityCodes.
        [Required, MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        // Higher = more severe; used for sorting and SLA lookups.
        public int Level { get; set; }

        [MaxLength(20)]
        public string? ColorCode { get; set; }

        public ICollection<SLAConfiguration> SLAConfigurations { get; set; } = new List<SLAConfiguration>();
    }
}
