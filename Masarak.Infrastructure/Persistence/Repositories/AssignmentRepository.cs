using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly Context _context;

        public AssignmentRepository(Context context)
        {
            _context = context;
        }

        public async Task<Assignment?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Include(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Class)
                .Include(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Subject)
                .FirstOrDefaultAsync(a => a.AssignmentId == id, ct);
        }

        public async Task<IEnumerable<Assignment>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Include(a => a.Submissions)
                .Where(a => a.AssignmentRef == taId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Assignment>> GetPublishedForClassAsync(int classId, int subjectId, CancellationToken ct = default)
        {
            return await _context.Assignments
                .Include(a => a.TeachingAssignment)
                .Where(a => a.TeachingAssignment.ClassId == classId && 
                            a.TeachingAssignment.SubjectId == subjectId &&
                            a.Status == Masarak.Domain.Enums.AssignmentStatus.Published)
                .OrderBy(a => a.DueDate)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Assignment assignment, CancellationToken ct = default)
        {
            await _context.Assignments.AddAsync(assignment, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Assignment assignment, CancellationToken ct = default)
        {
            _context.Assignments.Update(assignment);
            await _context.SaveChangesAsync(ct);
        }
    }
}
