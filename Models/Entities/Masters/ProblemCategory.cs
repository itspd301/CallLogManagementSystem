using System.ComponentModel.DataAnnotations;
using CallLogManagementSystem.Interfaces;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    public class ProblemCategory : MasterEntity, INamedMasterEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    }
}
