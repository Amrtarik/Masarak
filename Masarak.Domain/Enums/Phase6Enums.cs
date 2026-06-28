namespace Masarak.Domain.Enums
{
    /// <summary>
    /// Phase 6: Type of notification sent to a user.
    /// Covers all event-driven notification triggers across all phases.
    /// </summary>
    public enum NotificationType
    {
        // Student notifications
        NewAssignment,
        ExamOpening,
        ExamGraded,
        AttendanceAlert,
        NewContent,
        SubscriptionExpiring,
        SubscriptionExpired,

        // Teacher notifications
        NewEnrollment,
        SubmissionReceived,
        ExamFullyGraded,

        // Parent notifications
        MonthlyReportReady,
        StudentAttendanceAlert,
        StudentExamResult,

        // Admin notifications
        NewUserRegistered,
        PaymentFailed,
        LowPerformanceAlert,

        // General
        SubscriptionActivated
    }

    /// <summary>
    /// Phase 6: Delivery channel for notifications.
    /// Email is a Phase 7 stub only — currently only InApp is implemented.
    /// </summary>
    public enum NotificationChannel
    {
        InApp,
        Email  // Phase 7 stub — not implemented yet
    }
}
