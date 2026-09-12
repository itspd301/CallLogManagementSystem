using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    // Shopfloor/plant personnel who REPORT issues (Section 9, Field 9: "Problem Reported By").
    // Deliberately separate from ApplicationUser: most reporters (machine operators, shift
    // supervisors) never log into the system, so they carry no Identity/credentials.
    public class Employee : MasterEntity
    {
        [Required, MaxLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Designation { get; set; }

        public int LocationId { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }

        public int? ShopId { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }
    }
}
