using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.System;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string userId, int callLogId, NotificationType type, string message)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                CallLogId = callLogId,
                Type = type,
                Message = message,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
