using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for StudentExam (exam attempt) operations.
    /// </summary>
    public interface IStudentExamRepository
    {
        Task<StudentExam?> GetByStudentAndExamAsync(int studentId, int examId, CancellationToken ct = default);
        Task<StudentExam?> GetByIdWithAnswersAsync(int studentExamId, CancellationToken ct = default);
        Task<IEnumerable<StudentExam>> GetPendingManualGradingForTeacherAsync(int teacherUserId, CancellationToken ct = default);
        Task<IEnumerable<StudentExam>> GetExpiredInProgressAsync(DateTime now, CancellationToken ct = default);
        Task<IEnumerable<StudentExam>> GetGradedByStudentAndSubjectAsync(int studentId, int subjectId, CancellationToken ct = default);
        Task AddAsync(StudentExam attempt, CancellationToken ct = default);
        Task UpdateAsync(StudentExam attempt, CancellationToken ct = default);
    }
}
