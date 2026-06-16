namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Checks whether a user has an active subscription.
    /// Used by the SubscriptionAccessMiddleware and guards.
    /// </summary>
    public interface ISubscriptionAccessService
    {
        Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken ct = default);

        /// <summary>Invalidates the cached subscription status for a user.</summary>
        Task InvalidateCacheAsync(int userId);
    }
}
