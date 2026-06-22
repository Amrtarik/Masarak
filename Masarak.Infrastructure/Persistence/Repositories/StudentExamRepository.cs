using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class StudentExamRepository : IStudentExamRepository
    {
        private readonly Context _context;

        public StudentExamRepository(Context context)
        {
            _context = context;
        }

        public async Task<StudentExam?> GetByStudentAndExamAsync(int studentId, int examId, CancellationToken ct = default)
        {
            return await _context.StudentExams
                .Include(se => se.StudentAnswers)
                .FirstOrDefaultAsync(se => se.StudentId == studentId && se.ExamId == examId, ct);
        }

        public async Task<StudentExam?> GetByIdWithAnswersAsync(int studentExamId, CancellationToken ct = default)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions) // Need max marks for grading computation
                .Include(se => se.Student)
                .Include(se => se.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(se => se.StudentExamId == studentExamId, ct);
        }

        public async Task<IEnumerable<StudentExam>> GetPendingManualGradingForTeacherAsync(int teacherUserId, CancellationToken ct = default)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                .Include(se => se.StudentAnswers)
                .Where(se => se.Exam.TeachingAssignment.Teacher.UserId == teacherUserId && se.HasPendingManualGrading)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<StudentExam>> GetExpiredInProgressAsync(DateTime now, CancellationToken ct = default)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions)
                .Include(se => se.StudentAnswers)
                .Where(se => se.Status == Masarak.Domain.Enums.StudentExamStatus.InProgress && se.ExpiresAt <= now)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<StudentExam>> GetGradedByStudentAndSubjectAsync(int studentId, int subjectId, CancellationToken ct = default)
        {
            return await _context.StudentExams
                .Include(se => se.Exam)
                .Where(se => se.StudentId == studentId && 
                             se.Exam.TeachingAssignment.SubjectId == subjectId &&
                             se.Status == Masarak.Domain.Enums.StudentExamStatus.Graded)
                .ToListAsync(ct);
        }

        public async Task AddAsync(StudentExam attempt, CancellationToken ct = default)
        {
            await _context.StudentExams.AddAsync(attempt, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(StudentExam attempt, CancellationToken ct = default)
        {
            _context.StudentExams.Update(attempt);
            await _context.SaveChangesAsync(ct);
        }
    }
}
