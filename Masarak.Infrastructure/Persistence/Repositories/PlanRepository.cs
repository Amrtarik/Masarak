using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class PlanRepository : IPlanRepository
    {
        private readonly Context _context;

        public PlanRepository(Context context)
        {
            _context = context;
        }

        public async Task<Plan?> GetByIdAsync(int planId, CancellationToken ct = default)
        {
            return await _context.Plans.FindAsync(new object[] { planId }, ct);
        }

        public async Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken ct = default)
        {
            return await _context.Plans
                .Where(p => p.IsActive)
                .OrderBy(p => p.PriceMonthly)
                .ToListAsync(ct);
        }
    }
}
