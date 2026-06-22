namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Aggregated academic performance for a Student in a Subject for an AcademicYear.
    /// Unique per (StudentId, SubjectId, AcademicYear).
    ///
    /// Phase 3 additions:
    ///   • ClassId FK
    ///   • TotalExamsTaken, TotalAssignmentsSubmitted, TotalAssignmentsPending counters
    ///   • Recalculate() method for async performance updates
    /// </summary>
    public class StudentPerformance
    {
        public int      PerformanceId            { get; set; }
        public int      StudentId                { get; set; }   // FK → students.StudentId
        public int      SubjectId                { get; set; }   // FK → subjects.SubjectId
        public int?     ClassId                  { get; set; }   // FK → classes.ClassId
        public string   AcademicYear             { get; set; } = null!;
        public decimal  AvgAssignment            { get; set; } = 0;
        public decimal  AvgExam                  { get; set; } = 0;
        public decimal  AttendanceRate           { get; set; } = 0;
        public decimal? FinalGrade               { get; set; }
        public string?  GradeLetter              { get; set; }   // A+, A, B+, B …
        public string?  Remarks                  { get; set; }
        public int      TotalExamsTaken          { get; set; } = 0;
        public int      TotalAssignmentsSubmitted { get; set; } = 0;
        public int      TotalAssignmentsPending  { get; set; } = 0;
        public DateTime UpdatedAt                { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Student Student { get; set; } = null!;
        public virtual Subject Subject { get; set; } = null!;

        // ── Recalculate ─────────────────────────────────────────────────────
        /// <summary>
        /// Recalculates averages from graded exams and submissions.
        /// Called by the performance recalculation background consumer.
        /// </summary>
        public void Recalculate(IEnumerable<StudentExam> gradedExams, IEnumerable<Submission> gradedSubmissions,
            int totalAssignments)
        {
            var examList = gradedExams.ToList();
            var subList  = gradedSubmissions.ToList();

            TotalExamsTaken = examList.Count;
            TotalAssignmentsSubmitted = subList.Count;
            TotalAssignmentsPending = Math.Max(0, totalAssignments - subList.Count);

            if (examList.Any())
            {
                // Percentage: (sum of scores / sum of max) * 100
                var totalExamScore = examList.Sum(e => e.FinalScore ?? e.TotalScore ?? 0);
                var totalExamMax   = examList.Sum(e => e.Exam?.TotalMarks ?? e.Exam?.MaxScore ?? 100);
                AvgExam = totalExamMax > 0 ? (totalExamScore / totalExamMax) * 100 : 0;
            }

            if (subList.Any())
            {
                var totalSubScore = subList.Sum(s => s.Score ?? 0);
                var totalSubMax   = subList.Sum(s => s.Assignment?.MaxScore ?? 100);
                AvgAssignment = totalSubMax > 0 ? (totalSubScore / totalSubMax) * 100 : 0;
            }

            UpdatedAt = DateTime.UtcNow;
        }
    }
}

