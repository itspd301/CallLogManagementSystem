using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    // Cascades from both Module and ProblemCategory (Section 38).
    public class Problem : MasterEntity
    {
        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        public int ProblemCategoryId { get; set; }

        [ForeignKey(nameof(ProblemCategoryId))]
        public ProblemCategory? ProblemCategory { get; set; }

        public int? ModuleId { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public Module? Module { get; set; }
    }
}
