namespace Masarak.Domain.Enums
{
    /// <summary>Question format type — determines auto-grading capability.</summary>
    public enum QuestionType
    {
        MCQ,
        TrueFalse,
        ShortAnswer,
        Essay,
        FillInBlank,
        FileUpload
    }

    /// <summary>Difficulty level for questions and question bank filtering.</summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>Lifecycle states for an Assignment.</summary>
    public enum AssignmentStatus
    {
        Draft,
        Published,
        Closed
    }

    /// <summary>Lifecycle states for an Exam.</summary>
    public enum ExamStatus
    {
        Draft,
        Published,
        Closed
    }

    /// <summary>Lifecycle states for a student's exam attempt.</summary>
    public enum StudentExamStatus
    {
        InProgress,
        Submitted,
        AutoExpired,
        Graded
    }

    /// <summary>Lifecycle states for an assignment submission.</summary>
    public enum SubmissionStatus
    {
        Submitted,
        Graded
    }

    /// <summary>Grading state for individual student answers.</summary>
    public enum AnswerGradingStatus
    {
        AutoGraded,
        PendingReview,
        ManuallyGraded
    }
}
