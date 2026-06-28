namespace Masarak.Application.DTOs
{
    // ── Notification DTOs ─────────────────────────────────────────────────────

    /// <summary>
    /// Phase 6: Notification DTO for API responses and SignalR push.
    /// </summary>
    public record NotificationDto(
        int NotificationId,
        string Type,
        string Title,
        string Body,
        string? ActionUrl,
        bool IsRead,
        DateTime CreatedAt);

    // ── Admin User Management DTOs ────────────────────────────────────────────

    /// <summary>
    /// Phase 6: Admin user list item DTO.
    /// </summary>
    public record AdminUserDto(
        int UserId,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        DateTime CreatedAt,
        bool HasActiveSubscription);

    /// <summary>
    /// Phase 6: Detailed admin user view DTO with activity metrics.
    /// </summary>
    public record AdminUserDetailDto(
        int UserId,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        DateTime CreatedAt,
        bool HasActiveSubscription,
        int ExamsTaken,
        int AssignmentsSubmitted,
        decimal AttendancePercentage);

    // ── System Health DTO ─────────────────────────────────────────────────────

    /// <summary>
    /// Phase 6: System health metrics for admin dashboard.
    /// </summary>
    public record SystemHealthDto(
        int TotalUsers,
        int ActiveUsers,
        int TotalNotifications,
        int ActiveSubscriptions,
        int TotalContentItems,
        int TotalSessions,
        IEnumerable<string> RecentErrors);
}
