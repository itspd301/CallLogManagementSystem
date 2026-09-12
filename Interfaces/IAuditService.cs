using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Interfaces
{
    // Section 29 — every security-relevant or master/user-changing action goes through here.
    // Per-call field history (Section 19) is a separate concern, written to CallActivity by
    // ICallActivityService (Phase 20).
    public interface IAuditService
    {
        Task LogAsync(
            AuditAction action,
            string entityName,
            string? entityId = null,
            string? oldValue = null,
            string? newValue = null,
            string? userId = null);
    }
}
