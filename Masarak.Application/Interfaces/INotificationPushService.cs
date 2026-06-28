using Masarak.Application.DTOs;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Phase 6: Pushes real-time notifications via SignalR.
    /// Implemented by SignalRNotificationPushService.
    /// </summary>
    public interface INotificationPushService
    {
        /// <summary>Sends a real-time notification to a specific user via SignalR.</summary>
        Task PushToUserAsync(int userId, NotificationDto notification, CancellationToken ct);

        /// <summary>Sends a real-time notification to all users with a specific role.</summary>
        Task PushToRoleAsync(string role, NotificationDto notification, CancellationToken ct);
    }
}
