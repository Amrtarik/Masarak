namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Student profile — one per User whose Role.Name == "Student".
    ///
    /// Auth integration:
    ///   The JWT "userid" claim holds UserId.
    ///   Services resolve the matching Student row via:
    ///       db.Students.FirstOrDefault(s => s.UserId == currentUserId)
    ///   The [Authorize(Policy="StudentOnly")] guard ensures only Student JWTs reach
    ///   student-scoped endpoints.
    ///
    /// No structural changes from Phase 1.
    /// </summary>
    public class Student
    {
        public int      StudentId      { get; set; }
        public int      UserId         { get; set; }   // FK → users.UserId
        public int      GradeId        { get; set; }   // FK → grades.GradeId
        public DateTime EnrollmentDate { get; set; }
        public string   AcademicStatus { get; set; } = "Active";

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User    User  { get; set; } = null!;
        public virtual Grade   Grade { get; set; } = null!;
        public virtual ICollection<ParentStudent>      ParentStudents      { get; set; } = new List<ParentStudent>();
        public virtual ICollection<StudentClass>       StudentClasses      { get; set; } = new List<StudentClass>();
        public virtual ICollection<Submission>         Submissions         { get; set; } = new List<Submission>();
        public virtual ICollection<StudentExam>        StudentExams        { get; set; } = new List<StudentExam>();
        public virtual ICollection<StudentPerformance> StudentPerformances { get; set; } = new List<StudentPerformance>();
    }
}
