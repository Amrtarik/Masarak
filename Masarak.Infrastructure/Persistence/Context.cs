using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence
{
    /// <summary>
    /// Single DbContext for the entire Masarak platform.
    ///
    /// Phase 2 additions (marked ← NEW):
    ///   • DbSet&lt;RefreshToken&gt; RefreshTokens
    ///   • User entity gets three new columns + widened PasswordHash
    ///   • refresh_tokens table with unique index on Token
    ///
    /// All Phase 1 DbSets, relationships, Fluent API rules and delete behaviours
    /// are preserved verbatim.
    /// </summary>
    public class Context : DbContext
    {
        // ── Phase 1 DbSets ────────────────────────────────────────────────────
        public DbSet<Role>               Roles               { get; set; }
        public DbSet<User>               Users               { get; set; }
        public DbSet<Student>            Students            { get; set; }
        public DbSet<Teacher>            Teachers            { get; set; }
        public DbSet<Parent>             Parents             { get; set; }
        public DbSet<ParentStudent>      ParentStudents      { get; set; }
        public DbSet<Grade>              Grades              { get; set; }
        public DbSet<Class>              Classes             { get; set; }
        public DbSet<StudentClass>       StudentClasses      { get; set; }
        public DbSet<Subject>            Subjects            { get; set; }
        public DbSet<TeachingAssignment> TeachingAssignments { get; set; }
        public DbSet<Session>            Sessions            { get; set; }
        public DbSet<Attendance>         Attendances         { get; set; }
        public DbSet<Assignment>         Assignments         { get; set; }
        public DbSet<Submission>         Submissions         { get; set; }
        public DbSet<Exam>               Exams               { get; set; }
        public DbSet<Question>           Questions           { get; set; }
        public DbSet<StudentExam>        StudentExams        { get; set; }
        public DbSet<StudentAnswer>      StudentAnswers      { get; set; }
        public DbSet<StudentPerformance> StudentPerformances { get; set; }
        public DbSet<AiRecommendation>   AiRecommendations   { get; set; }
        public DbSet<Notification>       Notifications       { get; set; }
        public DbSet<Plan>               Plans               { get; set; }
        public DbSet<Subscription>       Subscriptions       { get; set; }
        public DbSet<Payment>            Payments            { get; set; }

        // ── Phase 2 DbSets ← NEW ──────────────────────────────────────────────
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // ── Phase 1 additions ─────────────────────────────────────────────────
        public DbSet<ParentStudentLink> ParentStudentLinks { get; set; }

        // ── Constructors ──────────────────────────────────────────────────────
        /// <summary>For ASP.NET Core dependency injection.</summary>
        public Context(DbContextOptions<Context> options) : base(options) { }

        /// <summary>Parameterless constructor for EF Core tooling (migrations).</summary>
        public Context() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback for EF tooling — real connection string comes from DI/appsettings
                optionsBuilder.UseSqlServer(
                    @"Data Source=HABIBA\SQLEXPRESS;Initial Catalog=EduPlatform;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ═══════════════════════════════════════════════════════════════════
            // 1. ROLE
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("roles");
                e.HasKey(x => x.RoleId);
                e.Property(x => x.RoleId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).HasMaxLength(50).IsRequired();
                e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_roles_Name");
            });

            // ═══════════════════════════════════════════════════════════════════
            // 2. USER  (Phase 2: 3 new columns + widened PasswordHash)
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(x => x.UserId);
                e.Property(x => x.UserId).ValueGeneratedOnAdd();

                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.Email).HasMaxLength(255).IsRequired();
                e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_users_Email");

                // Widened from 255 → 500 to hold PBKDF2-SHA512 "salt:hash" format
                e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

                e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.Country).HasMaxLength(100);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.IsActive).HasDefaultValue(true);

                // ── Phase 2 columns ← NEW ──────────────────────────────────
                e.Property(x => x.EmailConfirmed).HasDefaultValue(false);
                e.Property(x => x.FailedLoginCount).HasDefaultValue(0);
                e.Property(x => x.LockoutEnd).IsRequired(false);

                // ── Phase 1 additions ← Subscription phase ─────────────────
                e.Property(x => x.StudentLinkageCode).HasMaxLength(8);
                e.HasIndex(x => x.StudentLinkageCode)
                 .IsUnique()
                 .HasFilter("[StudentLinkageCode] IS NOT NULL")
                 .HasDatabaseName("UX_users_StudentLinkageCode");

                // ── Relationships ──────────────────────────────────────────
                e.HasOne(x => x.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(x => x.RoleId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 3. STUDENT
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Student>(e =>
            {
                e.ToTable("students");
                e.HasKey(x => x.StudentId);
                e.Property(x => x.StudentId).ValueGeneratedOnAdd();
                e.Property(x => x.EnrollmentDate).IsRequired();
                e.Property(x => x.AcademicStatus).HasMaxLength(50).HasDefaultValue("Active").IsRequired();
                e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("UX_students_UserId");

                e.HasOne(x => x.User)
                 .WithOne(u => u.Student)
                 .HasForeignKey<Student>(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Grade)
                 .WithMany(g => g.Students)
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 4. TEACHER
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Teacher>(e =>
            {
                e.ToTable("teachers");
                e.HasKey(x => x.TeacherId);
                e.Property(x => x.TeacherId).ValueGeneratedOnAdd();
                e.Property(x => x.Specialization).HasMaxLength(150);
                e.Property(x => x.HiringDate).IsRequired();
                e.Property(x => x.Bio).HasColumnType("nvarchar(max)");
                e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("UX_teachers_UserId");

                e.HasOne(x => x.User)
                 .WithOne(u => u.Teacher)
                 .HasForeignKey<Teacher>(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 5. PARENT
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Parent>(e =>
            {
                e.ToTable("parents");
                e.HasKey(x => x.ParentId);
                e.Property(x => x.ParentId).ValueGeneratedOnAdd();
                e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("UX_parents_UserId");

                e.HasOne(x => x.User)
                 .WithOne(u => u.Parent)
                 .HasForeignKey<Parent>(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 6. PARENT_STUDENT  (composite PK)
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<ParentStudent>(e =>
            {
                e.ToTable("parent_student");
                e.HasKey(x => new { x.ParentId, x.StudentId });
                e.Property(x => x.Relationship).HasMaxLength(50);

                e.HasOne(x => x.Parent)
                 .WithMany(p => p.ParentStudents)
                 .HasForeignKey(x => x.ParentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.ParentStudents)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 7. GRADE
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Grade>(e =>
            {
                e.ToTable("grades");
                e.HasKey(x => x.GradeId);
                e.Property(x => x.GradeId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_grades_Name");
                e.Property(x => x.Level).IsRequired();
            });

            // ═══════════════════════════════════════════════════════════════════
            // 8. CLASS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Class>(e =>
            {
                e.ToTable("classes");
                e.HasKey(x => x.ClassId);
                e.Property(x => x.ClassId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.Property(x => x.Capacity).HasDefaultValue(30);
                e.HasIndex(x => new { x.Name, x.GradeId })
                 .IsUnique().HasDatabaseName("UX_classes_Name_GradeId");

                e.HasOne(x => x.Grade)
                 .WithMany(g => g.Classes)
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 9. STUDENT_CLASS  (composite PK)
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<StudentClass>(e =>
            {
                e.ToTable("student_class");
                e.HasKey(x => new { x.StudentId, x.ClassId, x.AcademicYear });
                e.Property(x => x.AcademicYear).HasMaxLength(9).IsRequired();
                e.Property(x => x.IsCurrent).HasDefaultValue(true);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.StudentClasses)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Class)
                 .WithMany(c => c.StudentClasses)
                 .HasForeignKey(x => x.ClassId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 10. SUBJECT
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Subject>(e =>
            {
                e.ToTable("subjects");
                e.HasKey(x => x.SubjectId);
                e.Property(x => x.SubjectId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.Code).HasMaxLength(20).IsRequired();
                e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_subjects_Code");
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");

                e.HasOne(x => x.Grade)
                 .WithMany(g => g.Subjects)
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 11. TEACHING_ASSIGNMENTS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<TeachingAssignment>(e =>
            {
                e.ToTable("teaching_assignments");
                e.HasKey(x => x.AssignmentId);
                e.Property(x => x.AssignmentId).ValueGeneratedOnAdd();
                e.Property(x => x.AcademicYear).HasMaxLength(9).IsRequired();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.HasIndex(x => new { x.TeacherId, x.SubjectId, x.ClassId, x.AcademicYear })
                 .IsUnique().HasDatabaseName("UX_teaching_assignments_unique");

                e.HasOne(x => x.Teacher)
                 .WithMany(t => t.TeachingAssignments)
                 .HasForeignKey(x => x.TeacherId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Subject)
                 .WithMany(s => s.TeachingAssignments)
                 .HasForeignKey(x => x.SubjectId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Class)
                 .WithMany(c => c.TeachingAssignments)
                 .HasForeignKey(x => x.ClassId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 12. SESSIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Session>(e =>
            {
                e.ToTable("sessions");
                e.HasKey(x => x.SessionId);
                e.Property(x => x.SessionId).ValueGeneratedOnAdd();
                e.Property(x => x.Title).HasMaxLength(255).IsRequired();
                e.Property(x => x.StartTime).IsRequired();
                e.Property(x => x.EndTime).IsRequired();
                e.Property(x => x.MeetingLink).HasMaxLength(500);
                e.Property(x => x.RecordingUrl).HasMaxLength(500);
                e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Scheduled");

                e.HasOne(x => x.TeachingAssignment)
                 .WithMany(ta => ta.Sessions)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 13. ATTENDANCE
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Attendance>(e =>
            {
                e.ToTable("attendance");
                e.HasKey(x => x.AttendanceId);
                e.Property(x => x.AttendanceId).ValueGeneratedOnAdd();
                e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Present").IsRequired();
                e.HasIndex(x => new { x.SessionId, x.StudentId })
                 .IsUnique().HasDatabaseName("UX_attendance_Session_Student");

                e.HasOne(x => x.Session)
                 .WithMany(s => s.Attendances)
                 .HasForeignKey(x => x.SessionId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.Attendances)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 14. ASSIGNMENTS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Assignment>(e =>
            {
                e.ToTable("assignments");
                e.HasKey(x => x.AssignmentId);
                e.Property(x => x.AssignmentId).ValueGeneratedOnAdd();
                e.Property(x => x.Title).HasMaxLength(255).IsRequired();
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
                e.Property(x => x.DueDate).IsRequired();
                e.Property(x => x.MaxScore).HasColumnType("decimal(6,2)").HasDefaultValue(100m);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.TeachingAssignment)
                 .WithMany(ta => ta.Assignments)
                 .HasForeignKey(x => x.AssignmentRef)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 15. SUBMISSIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Submission>(e =>
            {
                e.ToTable("submissions");
                e.HasKey(x => x.SubmissionId);
                e.Property(x => x.SubmissionId).ValueGeneratedOnAdd();
                e.Property(x => x.SubmittedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.FileUrl).HasMaxLength(500);
                e.Property(x => x.AnswerText).HasColumnType("nvarchar(max)");
                e.Property(x => x.Score).HasColumnType("decimal(6,2)");
                e.Property(x => x.Feedback).HasColumnType("nvarchar(max)");
                e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Submitted");
                e.HasIndex(x => new { x.AssignmentId, x.StudentId })
                 .IsUnique().HasDatabaseName("UX_submissions_Assignment_Student");

                e.HasOne(x => x.Assignment)
                 .WithMany(a => a.Submissions)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.Submissions)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 16. EXAMS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Exam>(e =>
            {
                e.ToTable("exams");
                e.HasKey(x => x.ExamId);
                e.Property(x => x.ExamId).ValueGeneratedOnAdd();
                e.Property(x => x.Title).HasMaxLength(255).IsRequired();
                e.Property(x => x.StartTime).IsRequired();
                e.Property(x => x.EndTime).IsRequired();
                e.Property(x => x.DurationMins).IsRequired();
                e.Property(x => x.MaxScore).HasColumnType("decimal(6,2)").HasDefaultValue(100m);
                e.Property(x => x.ExamType).HasMaxLength(50).HasDefaultValue("Quiz");

                e.HasOne(x => x.TeachingAssignment)
                 .WithMany(ta => ta.Exams)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 17. QUESTIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Question>(e =>
            {
                e.ToTable("questions");
                e.HasKey(x => x.QuestionId);
                e.Property(x => x.QuestionId).ValueGeneratedOnAdd();
                e.Property(x => x.QuestionText).HasColumnType("nvarchar(max)").IsRequired();
                e.Property(x => x.QuestionType).HasMaxLength(30).IsRequired();
                e.Property(x => x.Options).HasColumnType("nvarchar(max)");
                e.Property(x => x.CorrectAns).HasColumnType("nvarchar(max)");
                e.Property(x => x.Marks).HasColumnType("decimal(5,2)").HasDefaultValue(1m);
                e.Property(x => x.OrderNum).HasDefaultValue(1);

                e.HasOne(x => x.Exam)
                 .WithMany(ex => ex.Questions)
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 18. STUDENT_EXAMS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<StudentExam>(e =>
            {
                e.ToTable("student_exams");
                e.HasKey(x => x.StudentExamId);
                e.Property(x => x.StudentExamId).ValueGeneratedOnAdd();
                e.Property(x => x.TotalScore).HasColumnType("decimal(6,2)");
                e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
                e.HasIndex(x => new { x.ExamId, x.StudentId })
                 .IsUnique().HasDatabaseName("UX_student_exams_Exam_Student");

                e.HasOne(x => x.Exam)
                 .WithMany(ex => ex.StudentExams)
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.StudentExams)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 19. STUDENT_ANSWERS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<StudentAnswer>(e =>
            {
                e.ToTable("student_answers");
                e.HasKey(x => x.AnswerId);
                e.Property(x => x.AnswerId).ValueGeneratedOnAdd();
                e.Property(x => x.AnswerText).HasColumnType("nvarchar(max)");
                e.Property(x => x.MarksAwarded).HasColumnType("decimal(5,2)");
                e.HasIndex(x => new { x.StudentExamId, x.QuestionId })
                 .IsUnique().HasDatabaseName("UX_student_answers_Exam_Question");

                e.HasOne(x => x.StudentExam)
                 .WithMany(se => se.StudentAnswers)
                 .HasForeignKey(x => x.StudentExamId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Question)
                 .WithMany(q => q.StudentAnswers)
                 .HasForeignKey(x => x.QuestionId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 20. STUDENT_PERFORMANCE
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<StudentPerformance>(e =>
            {
                e.ToTable("student_performance");
                e.HasKey(x => x.PerformanceId);
                e.Property(x => x.PerformanceId).ValueGeneratedOnAdd();
                e.Property(x => x.AcademicYear).HasMaxLength(9).IsRequired();
                e.Property(x => x.AvgAssignment).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
                e.Property(x => x.AvgExam).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
                e.Property(x => x.AttendanceRate).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
                e.Property(x => x.FinalGrade).HasColumnType("decimal(5,2)");
                e.Property(x => x.GradeLetter).HasMaxLength(2);
                e.Property(x => x.Remarks).HasColumnType("nvarchar(max)");
                e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
                e.HasIndex(x => new { x.StudentId, x.SubjectId, x.AcademicYear })
                 .IsUnique().HasDatabaseName("UX_student_performance_Student_Subject_Year");

                e.HasOne(x => x.Student)
                 .WithMany(s => s.StudentPerformances)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Subject)
                 .WithMany(s => s.StudentPerformances)
                 .HasForeignKey(x => x.SubjectId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 21. AI_RECOMMENDATIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<AiRecommendation>(e =>
            {
                e.ToTable("ai_recommendations");
                e.HasKey(x => x.RecommendationId);
                e.Property(x => x.RecommendationId).ValueGeneratedOnAdd();
                e.Property(x => x.GeneratedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.RecType).HasMaxLength(50).IsRequired();
                e.Property(x => x.ReferenceType).HasMaxLength(50);
                e.Property(x => x.Reason).HasColumnType("nvarchar(max)").IsRequired();
                e.Property(x => x.ActionUrl).HasMaxLength(500);
                e.Property(x => x.IsRead).HasDefaultValue(false);
                e.Property(x => x.IsDismissed).HasDefaultValue(false);

                e.HasOne(x => x.Student)
                 .WithMany(s => s.AiRecommendations)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 22. NOTIFICATIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("notifications");
                e.HasKey(x => x.NotificationId);
                e.Property(x => x.NotificationId).ValueGeneratedOnAdd();
                e.Property(x => x.Title).HasMaxLength(255).IsRequired();
                e.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
                e.Property(x => x.NotifType).HasMaxLength(50).IsRequired();
                e.Property(x => x.ReferenceType).HasMaxLength(50);
                e.Property(x => x.IsRead).HasDefaultValue(false);
                e.Property(x => x.SentAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 23. PLANS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Plan>(e =>
            {
                e.ToTable("plans");
                e.HasKey(x => x.PlanId);
                e.Property(x => x.PlanId).ValueGeneratedOnAdd();
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_plans_Name");
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
                e.Property(x => x.PriceMonthly).HasColumnType("decimal(10,2)").IsRequired();
                e.Property(x => x.PriceYearly).HasColumnType("decimal(10,2)");
                e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("USD").IsRequired();
                e.Property(x => x.DurationDays).HasDefaultValue(30);
                e.Property(x => x.MaxSubjects).HasDefaultValue(-1);
                e.Property(x => x.HasAi).HasDefaultValue(false);
                e.Property(x => x.HasLiveClass).HasDefaultValue(true);
                e.Property(x => x.HasRecordings).HasDefaultValue(true);
                e.Property(x => x.IsActive).HasDefaultValue(true);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 24. SUBSCRIPTIONS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Subscription>(e =>
            {
                e.ToTable("subscriptions");
                e.HasKey(x => x.SubscriptionId);
                e.Property(x => x.SubscriptionId).ValueGeneratedOnAdd();
                e.Property(x => x.StartDate).IsRequired();
                e.Property(x => x.EndDate).IsRequired();
                e.Property(x => x.BillingCycle).HasMaxLength(20).HasDefaultValue("Monthly").IsRequired();
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
                e.Property(x => x.ActivationMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
                e.Property(x => x.StripeSessionId).HasMaxLength(200);
                e.Property(x => x.StripeSubscriptionId).HasMaxLength(200);
                e.Property(x => x.AdminNote).HasMaxLength(500);
                e.Property(x => x.AutoRenew).HasDefaultValue(true);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_subscriptions_UserId_Status");
                e.HasIndex(x => x.StripeSessionId).HasDatabaseName("IX_subscriptions_StripeSessionId");

                e.HasOne(x => x.User)
                 .WithMany(u => u.Subscriptions)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Plan)
                 .WithMany(p => p.Subscriptions)
                 .HasForeignKey(x => x.PlanId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 25. PAYMENTS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<Payment>(e =>
            {
                e.ToTable("payments");
                e.HasKey(x => x.PaymentId);
                e.Property(x => x.PaymentId).ValueGeneratedOnAdd();
                e.Property(x => x.Amount).HasColumnType("decimal(10,2)").IsRequired();
                e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("USD").IsRequired();
                e.Property(x => x.Gateway).HasMaxLength(50).IsRequired();
                e.Property(x => x.GatewayTxnId).HasMaxLength(255);
                e.Property(x => x.StripePaymentIntentId).HasMaxLength(255);
                e.Property(x => x.StripeChargeId).HasMaxLength(255);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
                e.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Subscription)
                 .WithMany(s => s.Payments)
                 .HasForeignKey(x => x.SubscriptionId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 26. PARENT_STUDENT_LINKS
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<ParentStudentLink>(e =>
            {
                e.ToTable("parent_student_links");
                e.HasKey(x => x.ParentStudentLinkId);
                e.Property(x => x.ParentStudentLinkId).ValueGeneratedOnAdd();
                e.Property(x => x.LinkedAt).HasDefaultValueSql("GETDATE()");

                e.HasIndex(x => new { x.ParentUserId, x.StudentUserId })
                 .IsUnique()
                 .HasDatabaseName("UX_parent_student_links_Parent_Student");

                e.HasOne(x => x.Parent)
                 .WithMany(p => p.ParentStudentLinks)
                 .HasForeignKey(x => x.ParentUserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Student)
                 .WithMany() // Student only navigated through User if needed, or keeping it unidirectional
                 .HasForeignKey(x => x.StudentUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════
            // 26. REFRESH_TOKENS  ← Phase 2 NEW
            // ═══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("refresh_tokens");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                // Token must be unique — searched on every refresh request
                e.Property(x => x.Token).HasMaxLength(500).IsRequired();
                e.HasIndex(x => x.Token).IsUnique().HasDatabaseName("UX_refresh_tokens_Token");

                e.Property(x => x.JwtId).HasMaxLength(100).IsRequired();
                e.Property(x => x.IsUsed).HasDefaultValue(false);
                e.Property(x => x.IsRevoked).HasDefaultValue(false);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.ExpiresAt).IsRequired();
                e.Property(x => x.ReplacedByToken).HasMaxLength(500);
                e.Property(x => x.RevokedReason).HasMaxLength(255);

                // Index for fast per-user revocation queries
                e.HasIndex(x => x.UserId).HasDatabaseName("IX_refresh_tokens_UserId");
                // Index for expiry-based cleanup jobs
                e.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_refresh_tokens_ExpiresAt");

                e.HasOne(x => x.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
