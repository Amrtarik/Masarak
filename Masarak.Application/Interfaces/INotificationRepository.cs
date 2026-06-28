using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Phase 6: Repository for notification persistence.
    /// Handles paginated inbox queries, unread counts, and mark-read operations.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>Get paginated notifications for a user, optionally filtering to unread only.</summary>
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, int page, int pageSize, bool unreadOnly, CancellationToken ct);

        /// <summary>Get the total count of notifications for a user (for pagination).</summary>
        Task<int> GetTotalCountAsync(int userId, bool unreadOnly, CancellationToken ct);

        /// <summary>Get the count of unread notifications for the navbar badge.</summary>
        Task<int> GetUnreadCountAsync(int userId, CancellationToken ct);

        /// <summary>Persist a new notification to the database.</summary>
        Task AddAsync(Notification notification, CancellationToken ct);

        /// <summary>Mark a single notification as read (only if owned by userId).</summary>
        Task MarkReadAsync(int notificationId, int userId, CancellationToken ct);

        /// <summary>Mark all notifications for a user as read.</summary>
        Task MarkAllReadAsync(int userId, CancellationToken ct);
    }
}
