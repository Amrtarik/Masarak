namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Central identity entity. Serves as the authentication subject.
    ///
    /// Phase 2 changes (marked ← NEW):
    ///   • EmailConfirmed   – required for email-verification flow (Phase 3 sends the email;
    ///                        the column is created now so the schema doesn't change again)
    ///   • FailedLoginCount – persisted lockout counter (supplements in-memory lockout)
    ///   • LockoutEnd       – persisted lockout expiry (safe for multi-instance deployment)
    ///   • RefreshTokens    – navigation to child refresh-token rows
    ///
    /// All Phase 1 fields are preserved verbatim.
    /// </summary>
    public class User
    {
        // ── Phase 1 fields ──────────────────────────────────────────────────
        public int    UserId       { get; set; }
        public int    RoleId       { get; set; }
        public string FullName     { get; set; } = null!;
        public string Email        { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;   // PBKDF2-SHA512 (Phase 2)
        public string? Phone       { get; set; }
        public string? Country     { get; set; }
        public DateTime CreatedAt  { get; set; }
        public bool   IsActive     { get; set; } = true;

        // ── Phase 2 additions ← NEW ─────────────────────────────────────────
        /// <summary>True once the user has clicked the email-confirmation link.</summary>
        public bool      EmailConfirmed   { get; set; } = false;

        /// <summary>Running count of consecutive failed password attempts.</summary>
        public int       FailedLoginCount { get; set; } = 0;

        /// <summary>UTC datetime until which the account is locked out. Null = not locked.</summary>
        public DateTime? LockoutEnd       { get; set; }

        // ── Phase 1 additions ← Subscription phase ─────────────────────────
        /// <summary>8-char code generated for Student users, used by parents to link.</summary>
        public string? StudentLinkageCode { get; set; }

        // ── Password Reset ───────────────────────────────────────────────────
        /// <summary>Secure random token sent to the user for password reset.</summary>
        public string?   PasswordResetToken       { get; set; }
        /// <summary>UTC expiry for the reset token (1 hour from generation).</summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // ── Navigation ───────────────────────────────────────────────────────
        public virtual Role     Role     { get; set; } = null!;
        public virtual Student? Student  { get; set; }
        public virtual Teacher? Teacher  { get; set; }
        public virtual Parent?  Parent   { get; set; }
        public virtual ICollection<Notification>      Notifications      { get; set; } = new List<Notification>();
        public virtual ICollection<Subscription>      Subscriptions      { get; set; } = new List<Subscription>();
        public virtual ICollection<RefreshToken>      RefreshTokens      { get; set; } = new List<RefreshToken>();
        public virtual ICollection<ParentStudentLink> ParentStudentLinks { get; set; } = new List<ParentStudentLink>();
    }
}
