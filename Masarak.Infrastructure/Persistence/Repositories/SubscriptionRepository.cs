using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly Context _context;

        public SubscriptionRepository(Context context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByIdAsync(int subscriptionId, CancellationToken ct = default)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, ct);
        }

        public async Task<Subscription?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                // Just in case there are multiple, get the one ending latest
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Subscription?> GetByStripeSessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeSessionId == sessionId, ct);
        }

        public async Task<IEnumerable<Subscription>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Subscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < now)
                .ToListAsync(ct);
        }

        public async Task<(IEnumerable<Subscription> Items, int TotalCount)> GetAllPagedAsync(
            int pageNumber, int pageSize, SubscriptionStatus? status = null, CancellationToken ct = default)
        {
            var query = _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Plan)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        {
            await _context.Subscriptions.AddAsync(subscription, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync(ct);
        }
    }
}
