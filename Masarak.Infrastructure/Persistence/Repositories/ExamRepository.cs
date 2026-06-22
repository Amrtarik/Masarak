using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly Context _context;

        public ExamRepository(Context context)
        {
            _context = context;
        }

        public async Task<Exam?> GetByIdWithQuestionsAsync(int examId, CancellationToken ct = default)
        {
            return await _context.Exams
                .Include(e => e.TeachingAssignment)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.ExamId == examId, ct);
        }

        public async Task<IEnumerable<Exam>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct = default)
        {
            return await _context.Exams
                .Include(e => e.Questions)
                .Where(e => e.AssignmentId == taId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Exam>> GetOpenExamsForClassAsync(int classId, int subjectId, DateTime now, CancellationToken ct = default)
        {
            return await _context.Exams
                .Include(e => e.TeachingAssignment)
                .Include(e => e.Questions) // Need question count for DTOs
                .Where(e => e.TeachingAssignment.ClassId == classId &&
                            e.TeachingAssignment.SubjectId == subjectId &&
                            e.Status == Masarak.Domain.Enums.ExamStatus.Published &&
                            e.StartTime <= now && e.EndTime >= now)
                .OrderBy(e => e.StartTime)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Exam exam, CancellationToken ct = default)
        {
            await _context.Exams.AddAsync(exam, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Exam exam, CancellationToken ct = default)
        {
            _context.Exams.Update(exam);
            await _context.SaveChangesAsync(ct);
        }
    }
}
