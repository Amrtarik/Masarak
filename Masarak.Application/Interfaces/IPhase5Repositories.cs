using Masarak.Domain.Entities;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for AI-generated recommendations with TTL-based lifecycle.
    /// </summary>
    public interface IAiRecommendationRepository
    {
        Task<AiRecommendation?> GetActiveByStudentAndTypeAsync(int studentUserId, int? subjectId,
            RecommendationType type, CancellationToken ct);

        Task<IEnumerable<AiRecommendation>> GetActiveByStudentAsync(int studentUserId, CancellationToken ct);

        Task AddAsync(AiRecommendation rec, CancellationToken ct);

        Task DeactivateAsync(int studentUserId, int? subjectId, RecommendationType type, CancellationToken ct);
    }

    /// <summary>
    /// Repository for rule-based performance alerts.
    /// </summary>
    public interface IPerformanceAlertRepository
    {
        Task<PerformanceAlert?> GetUnresolvedAsync(int studentUserId, int? subjectId,
            AlertType type, CancellationToken ct);

        Task AddAsync(PerformanceAlert alert, CancellationToken ct);

        Task UpdateAsync(PerformanceAlert alert, CancellationToken ct);

        Task<IEnumerable<PerformanceAlert>> GetUnresolvedByStudentAsync(int studentUserId, CancellationToken ct);
    }

    /// <summary>
    /// Repository for admin-editable AI prompt templates.
    /// </summary>
    public interface IAiPromptTemplateRepository
    {
        Task<AiPromptTemplate?> GetByKeyAsync(string key, CancellationToken ct);

        Task<IEnumerable<AiPromptTemplate>> GetAllAsync(CancellationToken ct);

        Task UpdateAsync(AiPromptTemplate template, CancellationToken ct);
    }

    /// <summary>
    /// Repository for pre-computed analytics snapshots.
    /// </summary>
    public interface IAnalyticsSnapshotRepository
    {
        Task<AnalyticsDashboardSnapshot?> GetAsync(SnapshotScope scope, int? entityId, CancellationToken ct);

        Task UpsertAsync(AnalyticsDashboardSnapshot snapshot, CancellationToken ct);
    }
}
