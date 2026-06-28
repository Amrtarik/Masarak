using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Services
{
    /// <summary>
    /// Phase 6: Notification orchestration service.
    /// Creates a notification entity, persists to DB, maps to DTO, and pushes via SignalR.
    /// Called by RabbitMQ consumers — not exposed directly via API.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly INotificationPushService _pushService;
        private readonly Persistence.Context _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepo,
            INotificationPushService pushService,
            Persistence.Context context,
            ILogger<NotificationService> logger)
        {
            _notificationRepo = notificationRepo;
            _pushService = pushService;
            _context = context;
            _logger = logger;
        }

        public async Task CreateAndPushAsync(
            int userId, NotificationType type, string title, string body, string? actionUrl, CancellationToken ct)
        {
            // 1. Create Notification entity via factory method
            var notification = Notification.Create(userId, type, title, body, actionUrl);

            // 2. Persist via repository
            await _notificationRepo.AddAsync(notification, ct);

            _logger.LogInformation(
                "Phase 6: Notification created for User {UserId}: [{Type}] {Title}",
                userId, type, title);

            // 3. Map to DTO and push via SignalR
            var dto = new NotificationDto(
                notification.NotificationId,
                type.ToString(),
                title,
                body,
                actionUrl,
                false,
                notification.CreatedAt);

            try
            {
                await _pushService.PushToUserAsync(userId, dto, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to push notification via SignalR to User {UserId}. Notification persisted to DB.",
                    userId);
            }
        }

        public async Task CreateAndPushToRoleAsync(
            string role, NotificationType type, string title, string body, string? actionUrl, CancellationToken ct)
        {
            // Find all users with the specified role
            var users = await _context.Users
                .Where(u => u.Role.Name == role && u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync(ct);

            foreach (var userId in users)
            {
                await CreateAndPushAsync(userId, type, title, body, actionUrl, ct);
            }
        }
    }
}
