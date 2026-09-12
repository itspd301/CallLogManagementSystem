using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.AspNetCore.Http;

namespace CallLogManagementSystem.Interfaces
{
    public class AttachmentDeleteResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static AttachmentDeleteResult Success() => new() { Succeeded = true };
        public static AttachmentDeleteResult Failure(string error) => new() { Succeeded = false, Error = error };
    }

    public interface IAttachmentService
    {
        // Validates extension/MIME/size (Section 23/35) and saves each file under the
        // configured upload path, returning the CallAttachment rows created (not yet persisted
        // to CallLogId — caller adds them to the CallLog's Attachments collection / SaveChanges).
        Task<List<CallAttachment>> SaveAsync(int callLogId, IReadOnlyList<IFormFile>? files, string uploadedByUserId);

        // Removes the DB row and best-effort deletes the physical file. Ownership/role
        // authorization is the caller's responsibility (resource-based, like Assign/Handover).
        Task<AttachmentDeleteResult> DeleteAsync(int attachmentId);
    }
}
