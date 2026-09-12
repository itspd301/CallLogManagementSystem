using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.System;
using CallLogManagementSystem.Models.Enums;

namespace CallLogManagementSystem.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ICurrentUserService currentUser, ILogger<AuditService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task LogAsync(
            AuditAction action,
            string entityName,
            string? entityId = null,
            string? oldValue = null,
            string? newValue = null,
            string? userId = null)
        {
            var resolvedUserId = userId ?? _currentUser.UserId;

            var log = new AuditLog
            {
                UserId = resolvedUserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                IPAddress = _currentUser.IPAddress,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit: {Action} on {EntityName} {EntityId} by {UserId} from {IPAddress}",
                action, entityName, entityId, resolvedUserId, log.IPAddress);
        }
    }
}
