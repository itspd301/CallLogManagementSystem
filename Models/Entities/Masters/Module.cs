using System.ComponentModel.DataAnnotations;
using CallLogManagementSystem.Interfaces;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    public class Module : MasterEntity, ICodedMasterEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;
    }
}
