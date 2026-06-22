using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A homework/coursework assignment posted under a TeachingAssignment.
    ///
    /// Phase 3 additions:
    ///   • Status lifecycle (Draft → Published → Closed)
    ///   • Instructions field
    ///   • Factory method Create()
    ///   • Publish() / Close() state transitions
    ///
    /// Note: FK column name is "AssignmentRef" to avoid collision with AssignmentId PK.
    /// </summary>
    public class Assignment
    {
        public int               AssignmentId  { get; set; }
        public int               AssignmentRef { get; set; }   // FK → teaching_assignments.AssignmentId
        public string            Title         { get; set; } = null!;
        public string?           Description   { get; set; }
        public string?           Instructions  { get; set; }
        public DateTime          DueDate       { get; set; }
        public decimal           MaxScore      { get; set; } = 100;
        public AssignmentStatus  Status        { get; set; } = AssignmentStatus.Draft;
        public DateTime          CreatedAt     { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual TeachingAssignment      TeachingAssignment { get; set; } = null!;
        public virtual ICollection<Submission> Submissions        { get; set; } = new List<Submission>();

        // ── Factory ─────────────────────────────────────────────────────────
        public static Assignment Create(int assignmentRef, string title, string? instructions,
            DateTime dueDate, decimal maxScore)
        {
            return new Assignment
            {
                AssignmentRef = assignmentRef,
                Title         = title,
                Instructions  = instructions,
                DueDate       = dueDate,
                MaxScore      = maxScore,
                Status        = AssignmentStatus.Draft,
                CreatedAt     = DateTime.UtcNow
            };
        }

        public void Publish() { Status = AssignmentStatus.Published; }
        public void Close()   { Status = AssignmentStatus.Closed; }
    }
}
