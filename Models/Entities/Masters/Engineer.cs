using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    // A Master Configuration profile (Section 25) for IT/MES support staff, distinct from the
    // Users/Roles administration screen (Section 31 UserController). Deliberately does NOT
    // duplicate name/EmployeeId — those live once on ApplicationUser and are linked here so
    // "Attended By" / "Handed Over To" / assignment dropdowns query a single source of truth.
    public class Engineer : MasterEntity
    {
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        [MaxLength(150)]
        public string? Specialization { get; set; }

        public int? LocationId { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }
    }
}
