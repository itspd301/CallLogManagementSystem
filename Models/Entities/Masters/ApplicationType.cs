using System.ComponentModel.DataAnnotations;
using CallLogManagementSystem.Interfaces;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    public class ApplicationType : MasterEntity, INamedMasterEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
    }
}
