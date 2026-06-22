using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// An exam event under a TeachingAssignment.
    ///
    /// Phase 3 additions:
    ///   • ExamStatus lifecycle (Draft → Published → Closed)
    ///   • Instructions field
    ///   • TotalMarks computed from questions sum
    ///   • Factory method Create()
    ///   • Publish() validates questions exist
    ///   • RecalculateTotalMarks() sums question marks
    /// </summary>
    public class Exam
    {
        public int            ExamId       { get; set; }
        public int            AssignmentId { get; set; }   // FK → teaching_assignments.AssignmentId
        public string         Title        { get; set; } = null!;
        public string?        Instructions { get; set; }
        public DateTime       StartTime    { get; set; }
        public DateTime       EndTime      { get; set; }
        public int            DurationMins { get; set; }
        public decimal        MaxScore     { get; set; } = 100;
        public decimal        TotalMarks   { get; set; } = 0;        // computed from questions sum
        public ExamStatus     Status       { get; set; } = ExamStatus.Draft;
        public string         ExamType     { get; set; } = "Quiz";   // Quiz|MidTerm|Final|Practice
        public DateTime       CreatedAt    { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual TeachingAssignment         TeachingAssignment { get; set; } = null!;
        public virtual ICollection<Question>      Questions          { get; set; } = new List<Question>();
        public virtual ICollection<StudentExam>   StudentExams       { get; set; } = new List<StudentExam>();

        // ── Factory ─────────────────────────────────────────────────────────
        public static Exam Create(int assignmentId, string title, string? instructions,
            DateTime startTime, DateTime endTime, int durationMinutes)
        {
            return new Exam
            {
                AssignmentId = assignmentId,
                Title        = title,
                Instructions = instructions,
                StartTime    = startTime,
                EndTime      = endTime,
                DurationMins = durationMinutes,
                Status       = ExamStatus.Draft,
                CreatedAt    = DateTime.UtcNow
            };
        }

        public void Publish()
        {
            if (Questions == null || !Questions.Any())
                throw new InvalidOperationException("Cannot publish an exam with no questions.");
            RecalculateTotalMarks();
            Status = ExamStatus.Published;
        }

        public void Close() { Status = ExamStatus.Closed; }

        public void RecalculateTotalMarks()
        {
            TotalMarks = Questions?.Sum(q => q.Marks) ?? 0;
        }
    }
}

