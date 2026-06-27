using Masarak.Domain.Enums;

namespace Masarak.Application.DTOs
{
    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — AI Recommendation DTOs
    // ═══════════════════════════════════════════════════════════════════

    public record WeaknessAnalysisDto(
        int SubjectId,
        string SubjectName,
        IEnumerable<WeakTopicDto> WeakTopics,
        string NarrativeSummary,
        DateTime GeneratedAt);

    public record WeakTopicDto(
        string TopicName,
        decimal ErrorRate,
        IEnumerable<string> RecommendedActions);

    public record ContentRecommendationDto(
        int ContentItemId,
        string Title,
        string ContentType,
        string Reason,
        decimal RelevanceScore);

    public record ParentReportDto(
        string StudentName,
        string ReportMonth,
        decimal OverallScore,
        decimal AttendancePercentage,
        IEnumerable<SubjectSummaryDto> Subjects,
        string AiNarrative,
        IEnumerable<string> RecommendedActions,
        DateTime GeneratedAt);

    public record SubjectSummaryDto(
        string SubjectName,
        decimal AverageScore,
        decimal AttendancePercentage,
        string AiSubjectNarrative);

    public record TeachingSuggestionDto(
        string StudentName,
        string SubjectName,
        string Suggestion,
        IEnumerable<string> ActionItems,
        DateTime GeneratedAt);

    public record StudentInsightDto(
        string StudentName,
        string SubjectName,
        IEnumerable<WeakTopicDto> WeakTopics,
        IEnumerable<PerformanceAlertDto> ActiveAlerts,
        TeachingSuggestionDto? LatestSuggestion);


    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — Dashboard DTOs
    // ═══════════════════════════════════════════════════════════════════

    public record LearningInsightsDashboardDto(
        IEnumerable<WeaknessAnalysisDto> SubjectAnalyses,
        IEnumerable<ContentRecommendationDto> Recommendations,
        IEnumerable<PerformanceAlertDto> ActiveAlerts,
        IEnumerable<PerformanceTrendDto> PerformanceTrends);

    public record PerformanceTrendDto(
        string SubjectName,
        IEnumerable<ScorePointDto> ExamScores);

    public record ScorePointDto(
        DateTime Date,
        decimal Score);

    public record ClassAnalyticsDashboardDto(
        string ClassName,
        string SubjectName,
        decimal ClassAverage,
        IEnumerable<ScoreDistributionBucketDto> Distribution,
        IEnumerable<AnalyticsStudentScoreDto> TopFive,
        IEnumerable<AnalyticsStudentScoreDto> BottomFive,
        int SessionsScheduled,
        int SessionsCompleted);

    public record ScoreDistributionBucketDto(
        string Label,
        int Count);

    public record AnalyticsStudentScoreDto(
        int StudentUserId,
        string StudentName,
        decimal AverageScore);

    public record PlatformAnalyticsDto(
        int TotalActiveStudents,
        int TotalTeachers,
        int TotalActiveSubscriptions,
        decimal TotalRevenueThisMonth,
        decimal SessionCompletionRate,
        IEnumerable<GradeEnrollmentDto> EnrollmentByGrade);

    public record GradeEnrollmentDto(
        string GradeName,
        int StudentCount);

    public record GradeHeatmapDto(
        string GradeName,
        IEnumerable<ClassHeatmapRowDto> Classes);

    public record ClassHeatmapRowDto(
        string ClassName,
        IEnumerable<SubjectScoreCellDto> SubjectScores);

    public record SubjectScoreCellDto(
        string SubjectName,
        decimal AverageScore,
        string HeatmapColor);

    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — Alert DTOs
    // ═══════════════════════════════════════════════════════════════════

    public record PerformanceAlertDto(
        int PerformanceAlertId,
        int StudentUserId,
        string StudentName,
        int? SubjectId,
        string? SubjectName,
        AlertType AlertType,
        string Message,
        decimal TriggerValue,
        decimal Threshold,
        bool IsResolved,
        DateTime CreatedAt,
        DateTime? ResolvedAt);

    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — Prompt Template DTOs
    // ═══════════════════════════════════════════════════════════════════

    public record AiPromptTemplateDto(
        int AiPromptTemplateId,
        string Key,
        string SystemPrompt,
        string UserPromptTemplate,
        int MaxTokens,
        decimal Temperature,
        DateTime UpdatedAt,
        string UpdatedBy);

    public record UpdatePromptTemplateRequest(
        string SystemPrompt,
        string UserPromptTemplate,
        int MaxTokens,
        decimal Temperature);

    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — AI Provider DTOs
    // ═══════════════════════════════════════════════════════════════════

    public record AiPromptRequest(
        string SystemPrompt,
        string UserPrompt,
        int MaxTokens,
        decimal Temperature);

    public record AiCompletionResult(
        string Content,
        int PromptTokens,
        int CompletionTokens,
        string ProviderUsed);

    // ═══════════════════════════════════════════════════════════════════
    // Phase 5 — Generate Report Request
    // ═══════════════════════════════════════════════════════════════════

    public record GenerateParentReportRequest(
        string ReportMonth);

    public record GenerateTeachingSuggestionRequest(
        int SubjectId);
}
