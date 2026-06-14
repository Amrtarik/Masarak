using Masarak.Application.DTOs;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Application service handling all subscription-related operations.
    /// Follows the same service pattern as IAuthService.
    /// </summary>
    public interface ISubscriptionService
    {
        // ── Checkout ─────────────────────────────────────────────────────────
        Task<CheckoutResult> InitiateCheckoutAsync(int userId, InitiateCheckoutRequest request, CancellationToken ct = default);
        Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default);

        // ── Admin ────────────────────────────────────────────────────────────
        Task<SubscriptionDto> AdminActivateAsync(int adminId, AdminActivateRequest request, CancellationToken ct = default);
        Task AdminCancelAsync(int adminId, int subscriptionId, string reason, CancellationToken ct = default);
        Task<PagedResult<SubscriptionDto>> GetAllSubscriptionsAsync(int pageNumber, int pageSize, SubscriptionStatus? status, CancellationToken ct = default);

        // ── User ─────────────────────────────────────────────────────────────
        Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId, CancellationToken ct = default);
        Task<IEnumerable<SubscriptionDto>> GetSubscriptionHistoryAsync(int userId, CancellationToken ct = default);

        // ── Plans ────────────────────────────────────────────────────────────
        Task<IEnumerable<PlanDto>> GetAllPlansAsync(CancellationToken ct = default);

        // ── Parent-Student Linking ───────────────────────────────────────────
        Task<ParentStudentLinkDto> LinkParentToStudentAsync(int parentUserId, string studentLinkageCode, CancellationToken ct = default);
        Task<IEnumerable<LinkedStudentDto>> GetLinkedStudentsAsync(int parentUserId, CancellationToken ct = default);

        // ── Student Linkage Code ─────────────────────────────────────────────
        Task<string?> GetStudentLinkageCodeAsync(int userId, CancellationToken ct = default);
    }
}
