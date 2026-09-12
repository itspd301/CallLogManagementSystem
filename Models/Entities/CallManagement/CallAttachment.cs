using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallLogManagementSystem.Models.Entities.Identity;

namespace CallLogManagementSystem.Models.Entities.CallManagement
{
    // Section 23 — metadata only; the physical file lives under wwwroot/uploads
    // (or configurable storage per FileUpload:UploadPath).
    public class CallAttachment
    {
        public int Id { get; set; }

        public int CallLogId { get; set; }
        [ForeignKey(nameof(CallLogId))]
        public CallLog? CallLog { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        public string UploadedById { get; set; } = string.Empty;
        [ForeignKey(nameof(UploadedById))]
        public ApplicationUser? UploadedBy { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
