using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class AiRecommendationRepository : IAiRecommendationRepository
    {
        private readonly Context _context;

        public AiRecommendationRepository(Context context) => _context = context;

        public async Task<AiRecommendation?> GetActiveByStudentAndTypeAsync(
            int studentUserId, int? subjectId, RecommendationType type, CancellationToken ct)
        {
            return await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.SubjectId == subjectId
                         && r.Type == type
                         && r.IsActive)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<AiRecommendation>> GetActiveByStudentAsync(
            int studentUserId, CancellationToken ct)
        {
            return await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId && r.IsActive)
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync(ct);
        }

        public async Task AddAsync(AiRecommendation rec, CancellationToken ct)
        {
            await _context.AiRecommendations.AddAsync(rec, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeactivateAsync(int studentUserId, int? subjectId,
            RecommendationType type, CancellationToken ct)
        {
            var existing = await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.SubjectId == subjectId
                         && r.Type == type
                         && r.IsActive)
                .ToListAsync(ct);

            foreach (var rec in existing)
                rec.Deactivate();

            await _context.SaveChangesAsync(ct);
        }
    }

    public class PerformanceAlertRepository : IPerformanceAlertRepository
    {
        private readonly Context _context;

        public PerformanceAlertRepository(Context context) => _context = context;

        public async Task<PerformanceAlert?> GetUnresolvedAsync(
            int studentUserId, int? subjectId, AlertType type, CancellationToken ct)
        {
            return await _context.PerformanceAlerts
                .Where(a => a.StudentUserId == studentUserId
                         && a.SubjectId == subjectId
                         && a.AlertType == type
                         && !a.IsResolved)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddAsync(PerformanceAlert alert, CancellationToken ct)
        {
            await _context.PerformanceAlerts.AddAsync(alert, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(PerformanceAlert alert, CancellationToken ct)
        {
            _context.PerformanceAlerts.Update(alert);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<PerformanceAlert>> GetUnresolvedByStudentAsync(
            int studentUserId, CancellationToken ct)
        {
            return await _context.PerformanceAlerts
                .Include(a => a.Subject)
                .Where(a => a.StudentUserId == studentUserId && !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }
    }

    public class AiPromptTemplateRepository : IAiPromptTemplateRepository
    {
        private readonly Context _context;

        public AiPromptTemplateRepository(Context context) => _context = context;

        public async Task<AiPromptTemplate?> GetByKeyAsync(string key, CancellationToken ct)
        {
            return await _context.AiPromptTemplates
                .FirstOrDefaultAsync(t => t.Key == key, ct);
        }

        public async Task<IEnumerable<AiPromptTemplate>> GetAllAsync(CancellationToken ct)
        {
            return await _context.AiPromptTemplates
                .OrderBy(t => t.Key)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(AiPromptTemplate template, CancellationToken ct)
        {
            _context.AiPromptTemplates.Update(template);
            await _context.SaveChangesAsync(ct);
        }
    }

    public class AnalyticsSnapshotRepository : IAnalyticsSnapshotRepository
    {
        private readonly Context _context;

        public AnalyticsSnapshotRepository(Context context) => _context = context;

        public async Task<AnalyticsDashboardSnapshot?> GetAsync(
            SnapshotScope scope, int? entityId, CancellationToken ct)
        {
            return await _context.AnalyticsSnapshots
                .Where(s => s.Scope == scope && s.ScopeEntityId == entityId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task UpsertAsync(AnalyticsDashboardSnapshot snapshot, CancellationToken ct)
        {
            var existing = await GetAsync(snapshot.Scope, snapshot.ScopeEntityId, ct);
            if (existing != null)
            {
                existing.Refresh(snapshot.DataJson,
                    (int)(snapshot.ExpiresAt - snapshot.GeneratedAt).TotalHours);
            }
            else
            {
                await _context.AnalyticsSnapshots.AddAsync(snapshot, ct);
            }
            await _context.SaveChangesAsync(ct);
        }
    }
}
