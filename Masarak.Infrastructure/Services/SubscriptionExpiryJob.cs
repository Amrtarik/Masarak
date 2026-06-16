using Masarak.Application.Interfaces;
using Masarak.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Services
{
    public class SubscriptionExpiryJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionExpiryJob> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        public SubscriptionExpiryJob(IServiceProvider serviceProvider, ILogger<SubscriptionExpiryJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiry Job started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing subscription expirations.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task ProcessExpirationsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var accessService = scope.ServiceProvider.GetRequiredService<ISubscriptionAccessService>();

            var expiredSubs = await repo.GetExpiredActiveSubscriptionsAsync(stoppingToken);

            int count = 0;
            foreach (var sub in expiredSubs)
            {
                sub.Status = SubscriptionStatus.Expired;
                await repo.UpdateAsync(sub, stoppingToken);
                await accessService.InvalidateCacheAsync(sub.UserId);
                count++;
            }

            if (count > 0)
            {
                _logger.LogInformation("Expired {Count} subscriptions.", count);
            }
        }
    }
}
