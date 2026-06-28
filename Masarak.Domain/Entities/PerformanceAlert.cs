using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Rule-based performance or attendance alert for a student.
    /// Triggered automatically when thresholds are breached:
    ///   - LowAttendance: attendance drops below 75%
    ///   - LowExamScore: average exam score drops below 50%
    ///   - MissedAssignments: excessive unsubmitted assignments
    ///
    /// Alerts are surfaced to parents and consumed by Phase 6 Notifications.
    /// </summary>
    public class PerformanceAlert
    {
        public int       PerformanceAlertId { get; set; }
        public int       StudentUserId      { get; set; }  // FK → users.UserId
        public int?      SubjectId          { get; set; }  // FK → subjects.SubjectId (null for cross-subject alerts)
        public AlertType AlertType          { get; set; }
        public string    Message            { get; set; } = null!;
        public decimal   TriggerValue       { get; set; }  // the value that triggered (e.g., 45.0 for 45%)
        public decimal   Threshold          { get; set; }  // the threshold breached (e.g., 50.0)
        public bool      IsResolved         { get; set; } = false;
        public DateTime  CreatedAt          { get; set; }
        public DateTime? ResolvedAt         { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User    Student { get; set; } = null!;
        public virtual Subject? Subject { get; set; }

        // ── Domain Methods ──────────────────────────────────────────────────
        public static PerformanceAlert Create(int studentUserId, int? subjectId,
            AlertType alertType, string message, decimal triggerValue, decimal threshold)
        {
            return new PerformanceAlert
            {
                StudentUserId = studentUserId,
                SubjectId = subjectId,
                AlertType = alertType,
                Message = message,
                TriggerValue = triggerValue,
                Threshold = threshold,
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Resolve()
        {
            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
        }
    }
}
