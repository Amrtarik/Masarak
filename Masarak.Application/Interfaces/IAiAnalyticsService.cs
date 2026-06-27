using Masarak.Application.DTOs;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Service layer for AI recommendations, analytics, and rule-based alerts.
    /// Orchestrates AI provider calls, caching, and data aggregation.
    /// </summary>
    public interface IAiAnalyticsService
    {
        // ── Student-Facing ──────────────────────────────────────────────────
        Task<LearningInsightsDashboardDto> GetLearningInsightsAsync(int studentUserId, int academicYear, CancellationToken ct);
        Task<IEnumerable<ContentRecommendationDto>> GetRecommendedContentAsync(int studentUserId, int subjectId, CancellationToken ct);

        // ── Parent-Facing ───────────────────────────────────────────────────
        Task<ParentReportDto?> GetParentReportAsync(int parentUserId, int studentUserId, string reportMonth, CancellationToken ct);
        Task<ParentReportDto> GenerateParentReportAsync(int parentUserId, int studentUserId, string reportMonth, CancellationToken ct);
        Task<IEnumerable<PerformanceAlertDto>> GetStudentAlertsForParentAsync(int parentUserId, int studentUserId, CancellationToken ct);

        // ── Teacher-Facing ──────────────────────────────────────────────────
        Task<ClassAnalyticsDashboardDto> GetClassAnalyticsAsync(int teacherUserId, int classId, int subjectId, int academicYear, CancellationToken ct);
        Task<TeachingSuggestionDto> GenerateTeachingSuggestionAsync(int teacherUserId, int studentUserId, int subjectId, CancellationToken ct);
        Task<StudentInsightDto> GetStudentInsightAsync(int teacherUserId, int studentUserId, int subjectId, CancellationToken ct);


        // ── Admin-Facing ────────────────────────────────────────────────────
        Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync(int academicYear, CancellationToken ct);
        Task<GradeHeatmapDto> GetGradeHeatmapAsync(int gradeId, int academicYear, CancellationToken ct);
        Task<IEnumerable<AiPromptTemplateDto>> GetAllPromptTemplatesAsync(CancellationToken ct);
        Task UpdatePromptTemplateAsync(string key, UpdatePromptTemplateRequest request, string updatedBy, CancellationToken ct);

        // ── Rule-Based Alerts ───────────────────────────────────────────────
        Task<IEnumerable<PerformanceAlertDto>> EvaluatePerformanceAlertsAsync(int studentUserId, int subjectId, int classId, int academicYear, CancellationToken ct);

        // ── AI Generation (triggered by consumers) ──────────────────────────
        Task GenerateWeaknessAnalysisAsync(int studentUserId, int subjectId, int classId, CancellationToken ct);
    }
}
