using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A student's submission against an Assignment.
    /// Unique per (AssignmentId, StudentId) — one submission per assignment per student.
    ///
    /// Phase 3 additions:
    ///   • FileBlobName for Azure Blob signed URL generation
    ///   • GradedAt timestamp
    ///   • SubmissionStatus enum
    ///   • Factory method Create() and Grade() method
    /// </summary>
    public class Submission
    {
        public int               SubmissionId { get; set; }
        public int               AssignmentId { get; set; }   // FK → assignments.AssignmentId
        public int               StudentId    { get; set; }   // FK → students.StudentId
        public string?           AnswerText   { get; set; }
        public string?           FileUrl      { get; set; }           // Azure Blob public URL
        public string?           FileBlobName { get; set; }           // internal blob name for signed URL
        public SubmissionStatus  Status       { get; set; } = SubmissionStatus.Submitted;
        public decimal?          Score        { get; set; }
        public string?           Feedback     { get; set; }
        public DateTime          SubmittedAt  { get; set; }
        public DateTime?         GradedAt     { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Assignment Assignment { get; set; } = null!;
        public virtual Student    Student    { get; set; } = null!;

        // ── Factory ─────────────────────────────────────────────────────────
        public static Submission Create(int assignmentId, int studentId,
            string? answerText, string? fileUrl, string? fileBlobName)
        {
            return new Submission
            {
                AssignmentId = assignmentId,
                StudentId    = studentId,
                AnswerText   = answerText,
                FileUrl      = fileUrl,
                FileBlobName = fileBlobName,
                Status       = SubmissionStatus.Submitted,
                SubmittedAt  = DateTime.UtcNow
            };
        }

        public void Grade(decimal marks, string? feedback)
        {
            Score    = marks;
            Feedback = feedback;
            Status   = SubmissionStatus.Graded;
            GradedAt = DateTime.UtcNow;
        }
    }
}

