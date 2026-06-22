using Masarak.Domain.Entities;

namespace Masarak.Domain.Services
{
    /// <summary>
    /// Enforces exam time window rules on the server side.
    /// Called on any save-answer or submit request.
    /// </summary>
    public class ExamTimerEnforcer
    {
        /// <summary>
        /// Returns true if the student exam attempt is still within the allowed time window.
        /// </summary>
        public bool IsWithinTimeWindow(StudentExam attempt)
            => DateTime.UtcNow < attempt.ExpiresAt;
    }
}
