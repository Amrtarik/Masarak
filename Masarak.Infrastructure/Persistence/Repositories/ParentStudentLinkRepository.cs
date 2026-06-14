using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class ParentStudentLinkRepository : IParentStudentLinkRepository
    {
        private readonly Context _context;

        public ParentStudentLinkRepository(Context context)
        {
            _context = context;
        }

        public async Task<bool> LinkExistsAsync(int parentUserId, int studentUserId, CancellationToken ct = default)
        {
            return await _context.ParentStudentLinks
                .AnyAsync(l => l.ParentUserId == parentUserId && l.StudentUserId == studentUserId, ct);
        }

        public async Task AddAsync(ParentStudentLink link, CancellationToken ct = default)
        {
            await _context.ParentStudentLinks.AddAsync(link, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<ParentStudentLink>> GetByParentUserIdAsync(int parentUserId, CancellationToken ct = default)
        {
            return await _context.ParentStudentLinks
                .Include(l => l.Student)
                    .ThenInclude(s => s.Subscriptions.Where(sub => sub.Status == Domain.Enums.SubscriptionStatus.Active))
                .Where(l => l.ParentUserId == parentUserId)
                .ToListAsync(ct);
        }
    }
}
