using System.Security.Claims;
using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    /// <summary>
    /// Phase 6: Notification management endpoints.
    /// All endpoints require authentication — users can only access their own notifications.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Policy = AppPolicies.AnyAuthenticated)]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationsController(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        /// <summary>
        /// GET /api/notifications — Get paginated notifications for the current user.
        /// Query params: page, pageSize, unreadOnly
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false,
            CancellationToken ct = default)
        {
            var userId = GetUserId();

            var notifications = await _notificationRepo.GetByUserIdAsync(userId, page, pageSize, unreadOnly, ct);
            var totalCount = await _notificationRepo.GetTotalCountAsync(userId, unreadOnly, ct);

            var dtos = notifications.Select(n => new NotificationDto(
                n.NotificationId,
                n.Type.ToString(),
                n.Title,
                n.Body,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAt));

            Response.Headers["X-Total-Count"] = totalCount.ToString();

            return Ok(new
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = page < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = page > 1
            });
        }

        /// <summary>
        /// GET /api/notifications/unread-count — Get the unread notification count for the navbar badge.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
        {
            var userId = GetUserId();
            var count = await _notificationRepo.GetUnreadCountAsync(userId, ct);
            return Ok(new { UnreadCount = count });
        }

        /// <summary>
        /// PUT /api/notifications/{id}/read — Mark a single notification as read.
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id, CancellationToken ct = default)
        {
            var userId = GetUserId();
            await _notificationRepo.MarkReadAsync(id, userId, ct);
            return NoContent();
        }

        /// <summary>
        /// PUT /api/notifications/read-all — Mark all notifications as read for the current user.
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
        {
            var userId = GetUserId();
            await _notificationRepo.MarkAllReadAsync(userId, ct);
            return NoContent();
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");
    }
}
