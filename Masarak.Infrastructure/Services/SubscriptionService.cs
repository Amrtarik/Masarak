using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;

namespace Masarak.Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IParentStudentLinkRepository _linkRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStripeService _stripeService;
        private readonly ISubscriptionAccessService _accessService;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository,
            IPaymentRepository paymentRepository,
            IParentStudentLinkRepository linkRepository,
            IUserRepository userRepository,
            IStripeService stripeService,
            ISubscriptionAccessService accessService)
        {
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _paymentRepository = paymentRepository;
            _linkRepository = linkRepository;
            _userRepository = userRepository;
            _stripeService = stripeService;
            _accessService = accessService;
        }

        public async Task<CheckoutResult> InitiateCheckoutAsync(int userId, InitiateCheckoutRequest request, CancellationToken ct = default)
        {
            var plan = await _planRepository.GetByIdAsync(request.PlanId, ct);
            if (plan == null) throw new InvalidOperationException("Plan not found.");

            var activeSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, ct);
            if (activeSub != null) throw new InvalidOperationException("User already has an active subscription.");

            var (url, sessionId) = await _stripeService.CreateCheckoutSessionAsync(
                userId, plan.PlanId, plan.Name, plan.PriceMonthly, plan.Currency,
                request.SuccessUrl, request.CancelUrl, ct);

            var subscription = new Subscription
            {
                UserId = userId,
                PlanId = plan.PlanId,
                Status = SubscriptionStatus.Pending,
                ActivationMethod = ActivationMethod.Stripe,
                StripeSessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.DurationDays) // Will be updated on activation
            };

            await _subscriptionRepository.AddAsync(subscription, ct);
            return new CheckoutResult(url, sessionId);
        }

        public async Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default)
        {
            if (!_stripeService.ValidateWebhookSignature(payload, signature))
                throw new UnauthorizedAccessException("Invalid webhook signature.");

            var (eventType, sessionId, paymentIntentId) = _stripeService.ParseWebhookEvent(payload, signature);

            if (eventType == "checkout.session.completed" && sessionId != null)
            {
                var subscription = await _subscriptionRepository.GetByStripeSessionIdAsync(sessionId, ct);
                if (subscription != null && subscription.Status == SubscriptionStatus.Pending)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    subscription.StartDate = DateTime.UtcNow;
                    subscription.EndDate = DateTime.UtcNow.AddDays(subscription.Plan.DurationDays);
                    
                    await _subscriptionRepository.UpdateAsync(subscription, ct);

                    var payment = new Payment
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        Amount = subscription.Plan.PriceMonthly,
                        Currency = subscription.Plan.Currency,
                        Status = PaymentStatus.Completed,
                        Provider = PaymentProvider.Stripe,
                        StripePaymentIntentId = paymentIntentId,
                        CreatedAt = DateTime.UtcNow,
                        PaidAt = DateTime.UtcNow
                    };
                    await _paymentRepository.AddAsync(payment, ct);

                    _accessService.InvalidateCache(subscription.UserId);
                }
            }
        }

        public async Task<SubscriptionDto> AdminActivateAsync(int adminId, AdminActivateRequest request, CancellationToken ct = default)
        {
            var plan = await _planRepository.GetByIdAsync(request.PlanId, ct);
            if (plan == null) throw new InvalidOperationException("Plan not found.");

            var activeSub = await _subscriptionRepository.GetActiveByUserIdAsync(request.StudentUserId, ct);
            if (activeSub != null) throw new InvalidOperationException("Student already has an active subscription.");

            var subscription = new Subscription
            {
                UserId = request.StudentUserId,
                PlanId = plan.PlanId,
                Status = SubscriptionStatus.Active,
                ActivationMethod = ActivationMethod.AdminManual,
                ActivatedByAdminId = adminId,
                AdminNote = request.Note,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.DurationDays)
            };

            await _subscriptionRepository.AddAsync(subscription, ct);

            var payment = new Payment
            {
                SubscriptionId = subscription.SubscriptionId,
                Amount = plan.PriceMonthly,
                Currency = plan.Currency,
                Status = PaymentStatus.Completed,
                Provider = PaymentProvider.Manual,
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(payment, ct);

            _accessService.InvalidateCache(request.StudentUserId);
            
            // Reload with user to map DTO
            var loadedSub = await _subscriptionRepository.GetByIdAsync(subscription.SubscriptionId, ct);
            return MapToDto(loadedSub!);
        }

        public async Task AdminCancelAsync(int adminId, int subscriptionId, string reason, CancellationToken ct = default)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, ct);
            if (subscription == null) throw new InvalidOperationException("Subscription not found.");

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.AdminNote = $"Cancelled by Admin {adminId}. Reason: {reason}. Previous Note: {subscription.AdminNote}";
            
            await _subscriptionRepository.UpdateAsync(subscription, ct);
            _accessService.InvalidateCache(subscription.UserId);
        }

        public async Task<PagedResult<SubscriptionDto>> GetAllSubscriptionsAsync(int pageNumber, int pageSize, SubscriptionStatus? status, CancellationToken ct = default)
        {
            var (items, total) = await _subscriptionRepository.GetAllPagedAsync(pageNumber, pageSize, status, ct);
            return new PagedResult<SubscriptionDto>(
                items.Select(MapToDto), total, pageNumber, pageSize);
        }

        public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId, CancellationToken ct = default)
        {
            var sub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, ct);
            return sub != null ? MapToDto(sub) : null;
        }

        public async Task<IEnumerable<SubscriptionDto>> GetSubscriptionHistoryAsync(int userId, CancellationToken ct = default)
        {
            var subs = await _subscriptionRepository.GetByUserIdAsync(userId, ct);
            return subs.Select(MapToDto);
        }

        public async Task<IEnumerable<PlanDto>> GetAllPlansAsync(CancellationToken ct = default)
        {
            var plans = await _planRepository.GetAllActiveAsync(ct);
            return plans.Select(p => new PlanDto(
                p.PlanId, p.Name, p.Description, p.Type, p.PriceMonthly, p.Currency, 
                p.DurationDays, p.MaxSubjects, p.HasAi, p.HasLiveClass, p.HasRecordings));
        }

        public async Task<ParentStudentLinkDto> LinkParentToStudentAsync(int parentUserId, string studentLinkageCode, CancellationToken ct = default)
        {
            var studentUser = await _userRepository.GetByStudentLinkageCodeAsync(studentLinkageCode, ct);
            if (studentUser == null || studentUser.Role.Name != "Student")
                throw new InvalidOperationException("Invalid student linkage code.");

            if (await _linkRepository.LinkExistsAsync(parentUserId, studentUser.UserId, ct))
                throw new InvalidOperationException("Student is already linked to this parent.");

            var link = new ParentStudentLink
            {
                ParentUserId = parentUserId,
                StudentUserId = studentUser.UserId,
                LinkedAt = DateTime.UtcNow
            };

            await _linkRepository.AddAsync(link, ct);

            return new ParentStudentLinkDto(
                link.ParentStudentLinkId, parentUserId, studentUser.UserId, studentUser.FullName, link.LinkedAt);
        }

        public async Task<IEnumerable<LinkedStudentDto>> GetLinkedStudentsAsync(int parentUserId, CancellationToken ct = default)
        {
            var links = await _linkRepository.GetByParentUserIdAsync(parentUserId, ct);
            return links.Select(l => new LinkedStudentDto(
                l.StudentUserId, 
                l.Student.FullName, 
                l.Student.Email, 
                l.Student.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active)));
        }

        public async Task<string?> GetStudentLinkageCodeAsync(int userId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null || user.Role.Name != "Student") return null;

            if (string.IsNullOrEmpty(user.StudentLinkageCode))
            {
                user.StudentLinkageCode = Masarak.Domain.ValueObjects.StudentLinkageCode.Generate().Value;
                await _userRepository.UpdateAsync(user, ct);
            }
            return user.StudentLinkageCode;
        }

        private static SubscriptionDto MapToDto(Subscription s) => new(
            s.SubscriptionId,
            s.UserId,
            s.User?.FullName ?? "",
            s.Plan?.Name ?? "",
            s.Plan?.Type ?? PlanType.Monthly,
            s.Status,
            s.StartDate,
            s.EndDate,
            s.ActivationMethod,
            s.AdminNote
        );
    }
}
