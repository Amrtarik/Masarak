using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Pre-computed analytics snapshot cached in DB.
    /// Avoids expensive real-time joins for dashboard queries.
    ///
    /// Scope: Platform (global), Grade (per grade), Class (per class)
    /// DataJson contains serialized analytics payload.
    /// Regenerated on expiry by scheduled jobs.
    /// </summary>
    public class AnalyticsDashboardSnapshot
    {
        public int           SnapshotId    { get; set; }
        public SnapshotScope Scope         { get; set; }
        public int?          ScopeEntityId { get; set; }  // gradeId or classId if scoped
        public string        DataJson      { get; set; } = null!;  // serialized analytics payload
        public DateTime      GeneratedAt   { get; set; }
        public DateTime      ExpiresAt     { get; set; }

        // ── Domain Methods ──────────────────────────────────────────────────
        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

        public static AnalyticsDashboardSnapshot Create(SnapshotScope scope, int? entityId,
            string dataJson, int ttlHours)
        {
            return new AnalyticsDashboardSnapshot
            {
                Scope = scope,
                ScopeEntityId = entityId,
                DataJson = dataJson,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(ttlHours)
            };
        }

        public void Refresh(string dataJson, int ttlHours)
        {
            DataJson = dataJson;
            GeneratedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddHours(ttlHours);
        }
    }
}
