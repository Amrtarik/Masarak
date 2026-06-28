using Masarak.API.Hubs;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Masarak.API.Services
{
    /// <summary>
    /// Phase 6: Pushes real-time notifications to users via SignalR NotificationHub.
    /// Lives in API layer because it depends on the NotificationHub type.
    /// Uses user-specific groups ("user:{userId}") and role-based groups ("role:{role}").
    /// </summary>
    public class SignalRNotificationPushService : INotificationPushService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationPushService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushToUserAsync(int userId, NotificationDto notification, CancellationToken ct)
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync("ReceiveNotification", notification, ct);
        }

        public async Task PushToRoleAsync(string role, NotificationDto notification, CancellationToken ct)
        {
            await _hubContext.Clients
                .Group($"role:{role}")
                .SendAsync("ReceiveNotification", notification, ct);
        }
    }
}
