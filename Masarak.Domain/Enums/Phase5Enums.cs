namespace Masarak.Domain.Enums
{
    /// <summary>Type of AI-generated recommendation.</summary>
    public enum RecommendationType
    {
        WeaknessAnalysis,
        ContentRecommendation,
        ParentReport,
        TeachingSuggestion,
        ClassAnalytics
    }

    /// <summary>Type of rule-based performance alert.</summary>
    public enum AlertType
    {
        LowAttendance,
        LowExamScore,
        MissedAssignments
    }

    /// <summary>Scope level for analytics dashboard snapshots.</summary>
    public enum SnapshotScope
    {
        Platform,
        Grade,
        Class
    }

    /// <summary>Supported AI providers.</summary>
    public enum AiProvider
    {
        OpenAI,
        Claude,
        Gemini
    }
}
