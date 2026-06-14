namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Links a Parent user to a Student user via the student linkage code.
    /// This is the Phase 1 subscription-phase association (uses UserId directly,
    /// separate from the existing ParentStudent join which uses ParentId/StudentId).
    /// </summary>
    public class ParentStudentLink
    {
        public int      ParentStudentLinkId { get; set; }
        public int      ParentUserId        { get; set; }   // FK → users.UserId (parent)
        public int      StudentUserId       { get; set; }   // FK → users.UserId (student)
        public DateTime LinkedAt            { get; set; }

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User Parent  { get; set; } = null!;
        public virtual User Student { get; set; } = null!;
    }
}
