using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Masarak.API.Hubs
{
    /// <summary>
    /// Phase 6: SignalR hub for real-time notification delivery.
    /// Separate from ChatHub — purpose: server-push notifications only.
    /// Client methods: ReceiveNotification
    /// No client-invokable methods — this is a server-push-only hub.
    /// Groups: "user:{userId}" for user-specific notifications, "role:{role}" for role broadcasts.
    /// Endpoint: /hubs/notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// On connection: add user to personal group "user:{userId}" and role group "role:{role}".
        /// This enables both user-targeted and role-broadcast notifications.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue("userid");
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            }

            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// On disconnect: remove user from groups (handled automatically by SignalR).
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
