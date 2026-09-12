using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Interfaces
{
    public interface INotificationService
    {
        // Never throws on a missing/self-notify case — call-site guards (e.g. "don't notify the
        // person who performed the action") stay in the calling service, not here.
        Task CreateAsync(string userId, int callLogId, NotificationType type, string message);
    }
}
