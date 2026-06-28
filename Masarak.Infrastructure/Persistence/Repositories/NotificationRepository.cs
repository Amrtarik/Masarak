using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Phase 6: Notification repository implementation.
    /// Handles paginated inbox queries, unread badge count, and mark-read operations.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly Context _context;

        public NotificationRepository(Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(
            int userId, int page, int pageSize, bool unreadOnly, CancellationToken ct)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task<int> GetTotalCountAsync(int userId, bool unreadOnly, CancellationToken ct)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query.CountAsync(ct);
        }

        public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        }

        public async Task AddAsync(Notification notification, CancellationToken ct)
        {
            await _context.Notifications.AddAsync(notification, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task MarkReadAsync(int notificationId, int userId, CancellationToken ct)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, ct);

            if (notification != null)
            {
                notification.MarkRead();
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task MarkAllReadAsync(int userId, CancellationToken ct)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(n => n
                    .SetProperty(x => x.IsRead, true)
                    .SetProperty(x => x.ReadAt, DateTime.UtcNow), ct);
        }
    }
}
