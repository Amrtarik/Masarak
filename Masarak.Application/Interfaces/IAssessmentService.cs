using Masarak.Application.DTOs;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Core application service for Phase 3: Assessment & Grading.
    /// Manages assignments, exams, submissions, taking exams, and manual/auto grading.
    /// </summary>
    public interface IAssessmentService
    {
        // ── Teacher: Assignments ──────────────────────────────────────────────
        Task<AssignmentDto> CreateAssignmentAsync(int teacherUserId, CreateAssignmentRequest request, CancellationToken ct = default);
        Task PublishAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default);
        Task CloseAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default);
        Task<IEnumerable<AssignmentDto>> GetTeacherAssignmentsAsync(int teacherUserId, int teachingAssignmentId, CancellationToken ct = default);
        Task<IEnumerable<SubmissionDetailDto>> GetSubmissionsForAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default);
        Task<SubmissionDto> GradeSubmissionAsync(int teacherUserId, int submissionId, GradeSubmissionRequest request, CancellationToken ct = default);
        Task<string> GetSubmissionFileDownloadUrlAsync(int teacherUserId, int submissionId, CancellationToken ct = default);

        // ── Teacher: Exams ────────────────────────────────────────────────────
        Task<ExamDto> CreateExamAsync(int teacherUserId, CreateExamRequest request, CancellationToken ct = default);
        Task<QuestionDto> AddQuestionToExamAsync(int teacherUserId, int examId, AddQuestionRequest request, CancellationToken ct = default);
        Task<QuestionDto> UpdateQuestionAsync(int teacherUserId, int questionId, UpdateQuestionRequest request, CancellationToken ct = default);
        Task RemoveQuestionAsync(int teacherUserId, int questionId, CancellationToken ct = default);
        Task PublishExamAsync(int teacherUserId, int examId, CancellationToken ct = default);
        Task<IEnumerable<ExamDto>> GetTeacherExamsAsync(int teacherUserId, int teachingAssignmentId, CancellationToken ct = default);

        // ── Teacher: Grading Dashboard ────────────────────────────────────────
        Task<PendingGradingDashboardDto> GetPendingGradingDashboardAsync(int teacherUserId, CancellationToken ct = default);
        Task<ExamGradingReviewDto> GetStudentAnswersForReviewAsync(int teacherUserId, int studentExamId, CancellationToken ct = default);
        Task GradeStudentAnswerAsync(int teacherUserId, int answerId, GradeStudentAnswerRequest request, CancellationToken ct = default);

        // ── Student: Assignments ──────────────────────────────────────────────
        Task<IEnumerable<AssignmentDto>> GetStudentAssignmentsAsync(int studentUserId, int subjectId, CancellationToken ct = default);
        Task<SubmissionDto> SubmitAssignmentAsync(int studentUserId, int assignmentId, string? textContent, Stream? fileStream, string? fileName, string? contentType, CancellationToken ct = default);

        // ── Student: Exams ────────────────────────────────────────────────────
        Task<IEnumerable<ExamDto>> GetOpenExamsForStudentAsync(int studentUserId, int subjectId, CancellationToken ct = default);
        Task<ExamAttemptDto> StartExamAttemptAsync(int studentUserId, int examId, CancellationToken ct = default);
        Task SaveAnswerAsync(int studentUserId, int studentExamId, SaveAnswerRequest request, Stream? fileStream, string? fileName, string? contentType, CancellationToken ct = default);
        Task<ExamResultDto> SubmitExamAsync(int studentUserId, int studentExamId, CancellationToken ct = default);
        Task<ExamResultDto> GetStudentExamResultAsync(int studentUserId, int studentExamId, CancellationToken ct = default);

        // ── Performance & Reports ─────────────────────────────────────────────
        Task<IEnumerable<SubjectPerformanceDto>> GetStudentPerformanceAsync(int studentUserId, string academicYear, CancellationToken ct = default);
        Task<ClassPerformanceReportDto> GetClassPerformanceReportAsync(int classId, int subjectId, string academicYear, CancellationToken ct = default);
    }
}
