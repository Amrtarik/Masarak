using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly Context _context;

        public SubmissionRepository(Context context)
        {
            _context = context;
        }

        public async Task<Submission?> GetByIdAsync(int submissionId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);
        }

        public async Task<Submission?> GetByStudentAndAssignmentAsync(int studentId, int assignmentId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AssignmentId == assignmentId, ct);
        }

        public async Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Submission>> GetGradedByStudentAndSubjectAsync(int studentId, int subjectId, CancellationToken ct = default)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId == studentId && 
                            s.Assignment.TeachingAssignment.SubjectId == subjectId &&
                            s.Status == Masarak.Domain.Enums.SubmissionStatus.Graded)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Submission submission, CancellationToken ct = default)
        {
            await _context.Submissions.AddAsync(submission, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Submission submission, CancellationToken ct = default)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync(ct);
        }
    }
}
