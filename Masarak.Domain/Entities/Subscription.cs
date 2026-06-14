using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Records a User's subscription to a Plan over a billing period.
    ///
    /// Phase 1 additions:
    ///   • Status → SubscriptionStatus enum (Pending, Active, Expired, Cancelled)
    ///   • ActivationMethod → enum (Stripe, AdminManual, Cash)
    ///   • Stripe integration fields (StripeSessionId, StripeSubscriptionId)
    ///   • Admin manual activation fields (AdminNote, ActivatedByAdminId)
    /// </summary>
    public class Subscription
    {
        public int                SubscriptionId      { get; set; }
        public int                UserId              { get; set; }   // FK → users.UserId
        public int                PlanId              { get; set; }   // FK → plans.PlanId
        public SubscriptionStatus Status              { get; set; } = SubscriptionStatus.Pending;
        public DateTime           StartDate           { get; set; }
        public DateTime           EndDate             { get; set; }
        public ActivationMethod   ActivationMethod    { get; set; } = ActivationMethod.Stripe;
        public string?            StripeSessionId     { get; set; }
        public string?            StripeSubscriptionId { get; set; }
        public string?            AdminNote           { get; set; }
        public int?               ActivatedByAdminId  { get; set; }
        public string             BillingCycle        { get; set; } = "Monthly";   // Monthly|Yearly
        public bool               AutoRenew           { get; set; } = true;
        public DateTime           CreatedAt           { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User User { get; set; } = null!;
        public virtual Plan Plan { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
