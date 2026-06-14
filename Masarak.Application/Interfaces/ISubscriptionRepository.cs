using Masarak.Domain.Entities;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(int subscriptionId, CancellationToken ct = default);
        Task<Subscription?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);
        Task<Subscription?> GetByStripeSessionIdAsync(string sessionId, CancellationToken ct = default);
        Task<IEnumerable<Subscription>> GetByUserIdAsync(int userId, CancellationToken ct = default);
        Task<IEnumerable<Subscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken ct = default);
        Task<(IEnumerable<Subscription> Items, int TotalCount)> GetAllPagedAsync(
            int pageNumber, int pageSize, SubscriptionStatus? status = null, CancellationToken ct = default);
        Task AddAsync(Subscription subscription, CancellationToken ct = default);
        Task UpdateAsync(Subscription subscription, CancellationToken ct = default);
    }
}
