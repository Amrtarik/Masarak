using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Domain.Enums;
using Masarak.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Masarak.API.Controllers
{
    /// <summary>
    /// Phase 6: Admin system health endpoint.
    /// Provides platform-wide metrics for the admin dashboard.
    /// </summary>
    [ApiController]
    [Route("api/admin/system")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminSystemController : ControllerBase
    {
        private readonly Context _context;

        public AdminSystemController(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/admin/system/health — Get system health metrics.
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetSystemHealth(CancellationToken ct = default)
        {
            var totalUsers = await _context.Users.CountAsync(ct);
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive, ct);
            var totalNotifications = await _context.Notifications.CountAsync(ct);
            var activeSubscriptions = await _context.Subscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active, ct);
            var totalContentItems = await _context.ContentItems.CountAsync(ct);
            var totalSessions = await _context.Sessions.CountAsync(ct);

            var dto = new SystemHealthDto(
                totalUsers,
                activeUsers,
                totalNotifications,
                activeSubscriptions,
                totalContentItems,
                totalSessions,
                Enumerable.Empty<string>()); // RecentErrors — would come from structured logging in production

            return Ok(dto);
        }
    }
}
