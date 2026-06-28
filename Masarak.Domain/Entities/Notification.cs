using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Phase 6: In-app notification delivered to any User regardless of role.
    /// Refactored from Phase 1 stub to full implementation with:
    ///   - NotificationType enum (replaces string NotifType)
    ///   - NotificationChannel (InApp / Email stub)
    ///   - ActionUrl for deep linking
    ///   - Factory method + private setters (DDD compliance)
    ///   - Named MarkRead() method for state changes
    ///
    /// Auth integration:
    ///   Every authenticated user reads their own notifications:
    ///       db.Notifications.Where(n => n.UserId == currentUserId)
    ///   The "userid" JWT claim provides the filter key.
    ///   Only the owner can mark IsRead / set ReadAt (enforced in service layer).
    ///
    /// Soft delete strategy: notifications are never deleted per Master Roadmap rules.
    /// </summary>
    public class Notification
    {
        public int                 NotificationId { get; private set; }
        public int                 UserId         { get; private set; }   // FK → users.UserId
        public NotificationType    Type           { get; private set; }
        public string              Title          { get; private set; } = null!;
        public string              Body           { get; private set; } = null!;
        public string?             ActionUrl      { get; private set; }   // deep link: "/student/exams/42"
        public NotificationChannel Channel        { get; private set; }   // InApp, (Email stub for Phase 7)
        public bool                IsRead         { get; private set; }
        public DateTime            CreatedAt      { get; private set; }
        public DateTime?           ReadAt         { get; private set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User User { get; private set; } = null!;

        // ── Private parameterless constructor (for EF Core) ─────────────────
        private Notification() { }

        // ── Factory Method ──────────────────────────────────────────────────
        /// <summary>
        /// Creates a new in-app notification for a specific user.
        /// </summary>
        public static Notification Create(
            int userId,
            NotificationType type,
            string title,
            string body,
            string? actionUrl = null)
        {
            return new Notification
            {
                UserId    = userId,
                Type      = type,
                Title     = title,
                Body      = body,
                ActionUrl = actionUrl,
                Channel   = NotificationChannel.InApp,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ── Domain Methods ──────────────────────────────────────────────────
        /// <summary>
        /// Marks this notification as read with a timestamp.
        /// </summary>
        public void MarkRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
