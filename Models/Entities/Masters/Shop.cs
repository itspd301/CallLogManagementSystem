using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    // Cascades from Location (Section 38: "When Location changes -> Load Shops").
    public class Shop : MasterEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public int LocationId { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }
    }
}
