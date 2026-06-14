using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A subscription plan available to users.
    ///
    /// Phase 1 additions:
    ///   • Type (PlanType enum) — Monthly, PerSubject, FullCurriculum
    ///   • DurationDays — how long the plan lasts after activation
    ///
    /// All original Phase 1 fields are preserved.
    /// </summary>
    public class Plan
    {
        public int      PlanId          { get; set; }
        public string   Name            { get; set; } = null!;
        public string?  Description     { get; set; }
        public PlanType Type            { get; set; } = PlanType.Monthly;
        public decimal  PriceMonthly    { get; set; }
        public decimal? PriceYearly     { get; set; }
        public string   Currency        { get; set; } = "USD";
        public int      DurationDays    { get; set; } = 30;
        public int      MaxSubjects     { get; set; } = -1;   // -1 = unlimited
        public bool     HasAi           { get; set; } = false;
        public bool     HasLiveClass    { get; set; } = true;
        public bool     HasRecordings   { get; set; } = true;
        public bool     IsActive        { get; set; } = true;

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
