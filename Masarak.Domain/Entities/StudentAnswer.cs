using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A student's answer to a single Question within a StudentExam attempt.
    /// Unique per (StudentExamId, QuestionId).
    ///
    /// Phase 3 additions:
    ///   • SelectedOptionId for MCQ answers
    ///   • FileBlobName / FileUrl for file upload answers
    ///   • AnswerGradingStatus enum
    ///   • TeacherFeedback for manual grading
    ///   • AutoGrade() / ManualGrade() methods
    /// </summary>
    public class StudentAnswer
    {
        public int                  AnswerId       { get; set; }
        public int                  StudentExamId  { get; set; }   // FK → student_exams.StudentExamId
        public int                  QuestionId     { get; set; }   // FK → questions.QuestionId
        public string?              AnswerText     { get; set; }          // for text-based types
        public string?              SelectedOptionId { get; set; }        // for MCQ (option label e.g. "A")
        public string?              FileBlobName   { get; set; }          // for file upload answers
        public string?              FileUrl        { get; set; }
        public AnswerGradingStatus  GradingStatus  { get; set; } = AnswerGradingStatus.PendingReview;
        public bool?                IsCorrect      { get; set; }
        public decimal?             MarksAwarded   { get; set; }
        public string?              TeacherFeedback { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual StudentExam StudentExam { get; set; } = null!;
        public virtual Question    Question    { get; set; } = null!;

        // ── Grading Methods ─────────────────────────────────────────────────
        public void AutoGrade(string correctAnswer, decimal maxMarks)
        {
            string? studentAnswer = SelectedOptionId ?? AnswerText;
            if (studentAnswer != null && correctAnswer != null)
            {
                // Normalized comparison: trim, lowercase
                var normalizedStudent = studentAnswer.Trim().ToLowerInvariant();
                var normalizedCorrect = correctAnswer.Trim().ToLowerInvariant();
                IsCorrect = normalizedStudent == normalizedCorrect;
            }
            else
            {
                IsCorrect = false;
            }

            MarksAwarded  = IsCorrect == true ? maxMarks : 0;
            GradingStatus = AnswerGradingStatus.AutoGraded;
        }

        public void ManualGrade(decimal marks, string? feedback)
        {
            MarksAwarded    = marks;
            TeacherFeedback = feedback;
            IsCorrect       = marks > 0;
            GradingStatus   = AnswerGradingStatus.ManuallyGraded;
        }
    }
}

