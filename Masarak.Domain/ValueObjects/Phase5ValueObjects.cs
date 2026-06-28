namespace Masarak.Domain.ValueObjects
{
    /// <summary>AI-generated weakness analysis for a student in a subject.</summary>
    public record WeaknessAnalysisResult(
        IEnumerable<WeakTopic> WeakTopics,
        string NarrativeSummary,
        decimal OverallConfidenceScore);

    /// <summary>Individual weak topic identified by AI analysis.</summary>
    public record WeakTopic(
        string TopicName,
        decimal ErrorRate,
        string[] RecommendedActions);

    /// <summary>AI-recommended content item for a student.</summary>
    public record ContentRecommendationResult(
        int ContentItemId,
        string Title,
        string Reason,
        decimal RelevanceScore);

    /// <summary>AI-generated parent report data for a student's monthly performance.</summary>
    public record ParentReportData(
        string StudentName,
        string ReportMonth,
        decimal OverallPerformanceScore,
        decimal AttendancePercentage,
        IEnumerable<SubjectSummary> SubjectSummaries,
        string AiNarrative,
        IEnumerable<string> RecommendedActions);

    /// <summary>Per-subject summary within a parent report.</summary>
    public record SubjectSummary(
        string SubjectName,
        decimal AverageScore,
        decimal AttendancePercentage,
        string AiSubjectNarrative);
}
