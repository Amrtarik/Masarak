using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Masarak.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;

        public StripeService(IOptions<StripeSettings> options)
        {
            _settings = options.Value;
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public async Task<(string CheckoutUrl, string SessionId)> CreateCheckoutSessionAsync(
            int userId, int planId, string planName, decimal price, string currency, 
            string successUrl, string cancelUrl, CancellationToken ct = default)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmountDecimal = price * 100, // Stripe uses smallest currency unit
                            Currency = currency.ToLowerInvariant(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = planName,
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment", // Using 'payment' for Phase 1. Subscriptions can use 'subscription' mode later if we use Stripe Billing.
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = $"{userId}_{planId}",
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId.ToString() },
                    { "PlanId", planId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            return (session.Url, session.Id);
        }

        public bool ValidateWebhookSignature(string payload, string signature)
        {
            try
            {
                // This throws an exception if invalid
                EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
                return true;
            }
            catch (StripeException)
            {
                return false;
            }
        }

        public (string EventType, string? SessionId, string? PaymentIntentId) ParseWebhookEvent(string payload, string signature)
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                return (stripeEvent.Type, session?.Id, session?.PaymentIntentId);
            }

            return (stripeEvent.Type, null, null);
        }
    }
}
