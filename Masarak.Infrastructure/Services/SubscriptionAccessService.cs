using Masarak.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Masarak.Infrastructure.Services
{
    public class SubscriptionAccessService : ISubscriptionAccessService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IDistributedCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public SubscriptionAccessService(ISubscriptionRepository subscriptionRepository, IDistributedCache cache)
        {
            _subscriptionRepository = subscriptionRepository;
            _cache = cache;
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken ct = default)
        {
            var cacheKey = $"subscription:active:{userId}";

            var cachedValue = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                if (bool.TryParse(cachedValue, out bool isActive))
                {
                    return isActive;
                }
            }

            var activeSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, ct);
            var hasActive = activeSub != null;

            await _cache.SetStringAsync(
                cacheKey, 
                hasActive.ToString(), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                ct);

            return hasActive;
        }

        public async Task InvalidateCacheAsync(int userId)
        {
            var cacheKey = $"subscription:active:{userId}";
            await _cache.RemoveAsync(cacheKey);
        }
    }
}
