using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Records a Student's attempt at an Exam.
    /// Unique per (ExamId, StudentId) — one attempt per exam per student.
    ///
    /// Phase 3 additions:
    ///   • ExpiresAt for server-side timer enforcement
    ///   • TotalAutoScore / TotalManualScore / FinalScore breakdown
    ///   • HasPendingManualGrading flag
    ///   • StudentExamStatus enum
    ///   • Begin / Submit / ApplyAutoGrading / FinalizeMixedGrading methods
    /// </summary>
    public class StudentExam
    {
        public int                StudentExamId          { get; set; }
        public int                ExamId                 { get; set; }   // FK → exams.ExamId
        public int                StudentId              { get; set; }   // FK → students.StudentId
        public DateTime?          StartedAt              { get; set; }
        public DateTime?          SubmittedAt            { get; set; }
        public DateTime           ExpiresAt              { get; set; }   // StartedAt + DurationMinutes
        public StudentExamStatus  Status                 { get; set; } = StudentExamStatus.InProgress;
        public decimal?           TotalScore             { get; set; }   // legacy — still computed
        public decimal?           TotalAutoScore         { get; set; }
        public decimal?           TotalManualScore       { get; set; }
        public decimal?           FinalScore             { get; set; }   // auto + manual
        public bool               HasPendingManualGrading { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Exam    Exam    { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

        // ── Factory ─────────────────────────────────────────────────────────
        public static StudentExam Begin(int examId, int studentId, int durationMinutes)
        {
            var now = DateTime.UtcNow;
            return new StudentExam
            {
                ExamId    = examId,
                StudentId = studentId,
                StartedAt = now,
                ExpiresAt = now.AddMinutes(durationMinutes),
                Status    = StudentExamStatus.InProgress,
                HasPendingManualGrading = false
            };
        }

        public void MarkSubmitted()
        {
            SubmittedAt = DateTime.UtcNow;
            // Status will be set by grading logic (Submitted or Graded)
        }

        public void ApplyAutoGrading(decimal autoScore)
        {
            TotalAutoScore = autoScore;
        }

        public void FinalizeMixedGrading(decimal manualScore)
        {
            TotalManualScore = manualScore;
            FinalScore = (TotalAutoScore ?? 0) + manualScore;
            TotalScore = FinalScore; // keep legacy field in sync
            HasPendingManualGrading = false;
            Status = StudentExamStatus.Graded;
        }

        public void FinalizeFullAutoGrading(decimal autoScore)
        {
            TotalAutoScore = autoScore;
            FinalScore = autoScore;
            TotalScore = autoScore;
            HasPendingManualGrading = false;
            Status = StudentExamStatus.Graded;
        }
    }
}

