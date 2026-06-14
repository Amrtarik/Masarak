using Masarak.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Masarak.Infrastructure.Services
{
    public class SubscriptionAccessService : ISubscriptionAccessService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public SubscriptionAccessService(ISubscriptionRepository subscriptionRepository, IMemoryCache cache)
        {
            _subscriptionRepository = subscriptionRepository;
            _cache = cache;
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken ct = default)
        {
            var cacheKey = $"subscription:active:{userId}";

            if (_cache.TryGetValue(cacheKey, out bool isActive))
            {
                return isActive;
            }

            var activeSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, ct);
            var hasActive = activeSub != null;

            _cache.Set(cacheKey, hasActive, CacheDuration);
            return hasActive;
        }

        public void InvalidateCache(int userId)
        {
            var cacheKey = $"subscription:active:{userId}";
            _cache.Remove(cacheKey);
        }
    }
}
