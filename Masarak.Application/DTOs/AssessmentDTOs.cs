using System.ComponentModel.DataAnnotations;
using Masarak.Domain.Enums;

namespace Masarak.Application.DTOs
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Teacher - Assignment DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public record AssignmentDto(
        int AssignmentId, string Title, DateTime DueDate, decimal MaxScore, 
        AssignmentStatus Status, string SubjectName, string ClassName, int SubmissionCount);

    public record SubmissionDetailDto(
        int SubmissionId, int StudentId, string StudentName, SubmissionStatus Status, 
        decimal? Score, DateTime SubmittedAt, bool HasFile);

    public class CreateAssignmentRequest
    {
        [Required] public int TeachingAssignmentId { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; } = null!;
        public string? Instructions { get; set; }
        [Required] public DateTime DueDate { get; set; }
        [Range(0, 1000)] public decimal MaxScore { get; set; } = 100;
    }

    public class GradeSubmissionRequest
    {
        [Required, Range(0, 1000)] public decimal MarksAwarded { get; set; }
        public string? Feedback { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Teacher - Exam DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public record ExamDto(
        int ExamId, string Title, DateTime StartTime, DateTime EndTime, 
        int DurationMinutes, decimal TotalMarks, ExamStatus Status, int QuestionCount);

    public record QuestionDto(
        int QuestionId, QuestionType Type, string Text, decimal Marks, 
        DifficultyLevel Difficulty, int Order, IEnumerable<QuestionOptionDto>? Options);

    public record QuestionOptionDto(string Label, string Text);

    public class CreateExamRequest
    {
        [Required] public int TeachingAssignmentId { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; } = null!;
        public string? Instructions { get; set; }
        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }
        [Required, Range(1, 600)] public int DurationMinutes { get; set; }
    }

    public class AddQuestionRequest
    {
        [Required] public QuestionType Type { get; set; }
        [Required] public string Text { get; set; } = null!;
        [Range(0, 100)] public decimal Marks { get; set; } = 1;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
        public int Order { get; set; } = 1;
        public string? CorrectAnswer { get; set; }
        public string? ImageUrl { get; set; }
        public IEnumerable<QuestionOptionDto>? Options { get; set; }
    }

    public class UpdateQuestionRequest : AddQuestionRequest { }

    // ═══════════════════════════════════════════════════════════════════════════
    // Teacher - Grading Dashboard DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public record PendingGradingDashboardDto(
        int TotalPendingExamAnswers, 
        int TotalPendingSubmissions, 
        IEnumerable<PendingExamSummaryDto> Exams, 
        IEnumerable<PendingAssignmentSummaryDto> Assignments);

    public record PendingExamSummaryDto(int ExamId, string ExamTitle, int PendingAnswersCount);
    public record PendingAssignmentSummaryDto(int AssignmentId, string AssignmentTitle, int PendingSubmissionsCount);

    public record ExamGradingReviewDto(
        int StudentExamId, string StudentName, string ExamTitle, 
        decimal TotalAutoScore, IEnumerable<PendingAnswerDto> PendingAnswers);

    public record PendingAnswerDto(
        int AnswerId, int QuestionId, string QuestionText, decimal MaxMarks, 
        string? AnswerText, string? FileUrl);

    public class GradeStudentAnswerRequest
    {
        [Required, Range(0, 1000)] public decimal MarksAwarded { get; set; }
        public string? Feedback { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Student - Assignment DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public class SubmitAssignmentRequest
    {
        public string? TextContent { get; set; }
        // Note: For file uploads, API controller handles IFormFile, service takes Stream + filename
    }

    public record SubmissionDto(
        int SubmissionId, int AssignmentId, SubmissionStatus Status, 
        DateTime SubmittedAt, decimal? Score, string? Feedback, string? FileUrl);

    // ═══════════════════════════════════════════════════════════════════════════
    // Student - Exam DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public record ExamAttemptDto(
        int StudentExamId, int ExamId, string ExamTitle, DateTime ExpiresAt, 
        int SecondsRemaining, IEnumerable<QuestionDto> Questions, 
        IEnumerable<SavedAnswerDto> SavedAnswers);

    public record SavedAnswerDto(int QuestionId, string? AnswerText, string? SelectedOptionId);

    public class SaveAnswerRequest
    {
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public string? SelectedOptionId { get; set; }
        // File handling separate
    }

    public record ExamResultDto(
        int StudentExamId, decimal FinalScore, decimal TotalMarks, decimal Percentage, 
        bool HasPendingManualGrading, IEnumerable<AnswerResultDto> Answers);

    public record AnswerResultDto(
        int QuestionId, string QuestionText, string? YourAnswer, string? CorrectAnswer, 
        decimal MarksAwarded, decimal MaxMarks, AnswerGradingStatus GradingStatus, string? Feedback);

    // ═══════════════════════════════════════════════════════════════════════════
    // Performance DTOs
    // ═══════════════════════════════════════════════════════════════════════════
    public record SubjectPerformanceDto(
        int SubjectId, string SubjectName, decimal AverageExamScore, 
        decimal AverageAssignmentScore, int TotalExamsTaken, int TotalAssignmentsSubmitted);

    public record ClassPerformanceReportDto(
        int ClassId, string ClassName, string SubjectName, 
        decimal ClassAverageScore, IEnumerable<StudentScoreDto> StudentScores);

    public record StudentScoreDto(int StudentId, string StudentName, decimal FinalScore);
}
