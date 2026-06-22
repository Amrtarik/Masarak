using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Messaging
{
    /// <summary>
    /// Shared logic for recalculating student performance.
    /// </summary>
    internal static class PerformanceRecalculator
    {
        public static async Task RecalculateForStudentAsync(
            Persistence.Context context,
            IStudentPerformanceRepository performanceRepo,
            int studentUserId, int subjectId, int classId, string academicYear, CancellationToken ct)
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.UserId == studentUserId, ct);
            if (student == null) return;

            var exams = await context.StudentExams
                .Include(se => se.Exam).ThenInclude(e => e.TeachingAssignment)
                .Where(se => se.StudentId == student.StudentId &&
                             se.Exam.TeachingAssignment.SubjectId == subjectId &&
                             se.Status == Masarak.Domain.Enums.StudentExamStatus.Graded)
                .ToListAsync(ct);

            var submissions = await context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.TeachingAssignment)
                .Where(s => s.StudentId == student.StudentId &&
                            s.Assignment.TeachingAssignment.SubjectId == subjectId &&
                            s.Status == Masarak.Domain.Enums.SubmissionStatus.Graded)
                .ToListAsync(ct);

            var performance = await performanceRepo.GetByStudentSubjectClassAsync(student.StudentId, subjectId, academicYear, ct);
            if (performance == null)
            {
                performance = new StudentPerformance
                {
                    StudentId = student.StudentId,
                    SubjectId = subjectId,
                    ClassId = classId,
                    AcademicYear = academicYear
                };
            }

            var totalAssignments = await context.Assignments
                .CountAsync(a => a.TeachingAssignment.SubjectId == subjectId &&
                                 a.TeachingAssignment.ClassId == classId &&
                                 a.Status == Masarak.Domain.Enums.AssignmentStatus.Published, ct);

            performance.Recalculate(exams, submissions, totalAssignments);
            await performanceRepo.UpsertAsync(performance, ct);
        }
    }

    /// <summary>
    /// Consumes ExamFullyGradedEvent to recalculate student performance.
    /// </summary>
    public class ExamGradedConsumer : IConsumer<ExamFullyGradedEvent>
    {
        private readonly IStudentPerformanceRepository _performanceRepo;
        private readonly Persistence.Context _context;
        private readonly ILogger<ExamGradedConsumer> _logger;

        public ExamGradedConsumer(
            IStudentPerformanceRepository performanceRepo,
            Persistence.Context context,
            ILogger<ExamGradedConsumer> logger)
        {
            _performanceRepo = performanceRepo;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ExamFullyGradedEvent> context)
        {
            _logger.LogInformation("Recalculating performance for Student {StudentUserId} after StudentExam {StudentExamId} graded.",
                context.Message.StudentUserId, context.Message.StudentExamId);
            await PerformanceRecalculator.RecalculateForStudentAsync(
                _context, _performanceRepo,
                context.Message.StudentUserId, context.Message.SubjectId, context.Message.ClassId,
                "2026-2027", context.CancellationToken);
        }
    }

    /// <summary>
    /// Consumes AssignmentGradedEvent to recalculate student performance.
    /// </summary>
    public class AssignmentGradedConsumer : IConsumer<AssignmentGradedEvent>
    {
        private readonly IStudentPerformanceRepository _performanceRepo;
        private readonly Persistence.Context _context;
        private readonly ILogger<AssignmentGradedConsumer> _logger;

        public AssignmentGradedConsumer(
            IStudentPerformanceRepository performanceRepo,
            Persistence.Context context,
            ILogger<AssignmentGradedConsumer> logger)
        {
            _performanceRepo = performanceRepo;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AssignmentGradedEvent> context)
        {
            _logger.LogInformation("Recalculating performance for Student {StudentUserId} after Submission {SubmissionId} graded.",
                context.Message.StudentUserId, context.Message.SubmissionId);
            await PerformanceRecalculator.RecalculateForStudentAsync(
                _context, _performanceRepo,
                context.Message.StudentUserId, context.Message.SubjectId, context.Message.ClassId,
                "2026-2027", context.CancellationToken);
        }
    }
}
