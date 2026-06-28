namespace Masarak.Domain.Events
{
    // ── Phase 1 Events ──────────────────────────────────────────────────
    /// <summary>Raised when a subscription transitions to Active status.</summary>
    public record SubscriptionActivatedEvent(int UserId, int SubscriptionId, DateTime EndDate);

    /// <summary>Raised when a subscription transitions to Expired status.</summary>
    public record SubscriptionExpiredEvent(int UserId, int SubscriptionId);

    /// <summary>Raised when a parent is successfully linked to a student.</summary>
    public record ParentStudentLinkedEvent(int ParentUserId, int StudentUserId);

    // ── Phase 3 Events ──────────────────────────────────────────────────
    /// <summary>Raised when a student submits an exam (or exam auto-expires).</summary>
    public record ExamSubmittedEvent(int StudentExamId, int StudentUserId, int ExamId);

    /// <summary>Raised when all answers in a student exam are graded (auto + manual).</summary>
    public record ExamFullyGradedEvent(int StudentExamId, int StudentUserId, int SubjectId, int ClassId);

    /// <summary>Raised when a teacher grades an assignment submission.</summary>
    public record AssignmentGradedEvent(int SubmissionId, int StudentUserId, int SubjectId, int ClassId);

    /// <summary>Raised after performance metrics are recalculated.</summary>
    public record PerformanceRecalculatedEvent(int StudentUserId, int SubjectId, int ClassId);

    // ── Phase 5 Events ──────────────────────────────────────────────────
    /// <summary>Raised when a performance alert is created for a student (consumed by Phase 6 Notifications).</summary>
    public record AlertCreatedEvent(int StudentUserId, int? SubjectId, Enums.AlertType AlertType, string Message);

    /// <summary>Raised when a parent report has been generated and is ready to view.</summary>
    public record ParentReportReadyEvent(int StudentUserId, string ReportMonth);
}

