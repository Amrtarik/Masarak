using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A payment transaction linked to a Subscription.
    ///
    /// Phase 1 additions:
    ///   • Status → PaymentStatus enum (Pending, Completed, Failed, Refunded)
    ///   • Provider → PaymentProvider enum (Stripe, Manual)
    ///   • StripePaymentIntentId, StripeChargeId — Stripe-specific correlation
    ///
    /// Gateway/GatewayTxnId from original schema are preserved for backward compat.
    /// </summary>
    public class Payment
    {
        public int            PaymentId            { get; set; }
        public int            SubscriptionId       { get; set; }   // FK → subscriptions.SubscriptionId
        public decimal        Amount               { get; set; }
        public string         Currency             { get; set; } = "USD";
        public PaymentStatus  Status               { get; set; } = PaymentStatus.Pending;
        public PaymentProvider Provider             { get; set; } = PaymentProvider.Stripe;
        public string         Gateway              { get; set; } = null!;    // e.g. "Stripe", "Manual"
        public string?        GatewayTxnId         { get; set; }
        public string?        StripePaymentIntentId { get; set; }
        public string?        StripeChargeId       { get; set; }
        public DateTime?      PaidAt               { get; set; }
        public DateTime       CreatedAt            { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Subscription Subscription { get; set; } = null!;
    }
}
