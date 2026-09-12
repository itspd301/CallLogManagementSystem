using System.ComponentModel.DataAnnotations;

namespace CallLogManagementSystem.Models.Entities.Masters
{
    public class Status : MasterEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Stable key from Constants.StatusCodes — business logic and workflow transitions
        // key off this, never off Name (which admins can rename).
        [Required, MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ColorCode { get; set; }

        public int SortOrder { get; set; }
    }
}
