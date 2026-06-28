using Masarak.Application.DTOs;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Phase 6: Notification orchestration service.
    /// Creates notifications, persists them, and pushes via SignalR in a single operation.
    /// Called by RabbitMQ consumers — not directly exposed via API.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Creates a notification entity, persists to DB, and pushes via SignalR to the target user.
        /// </summary>
        Task CreateAndPushAsync(int userId, NotificationType type, string title, string body, string? actionUrl, CancellationToken ct);

        /// <summary>
        /// Creates and pushes a notification to all users with a specific role (e.g. Admin).
        /// </summary>
        Task CreateAndPushToRoleAsync(string role, NotificationType type, string title, string body, string? actionUrl, CancellationToken ct);
    }
}
