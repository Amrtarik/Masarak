using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for assignment Submission operations.
    /// </summary>
    public interface ISubmissionRepository
    {
        Task<Submission?> GetByIdAsync(int submissionId, CancellationToken ct = default);
        Task<Submission?> GetByStudentAndAssignmentAsync(int studentId, int assignmentId, CancellationToken ct = default);
        Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken ct = default);
        Task<IEnumerable<Submission>> GetGradedByStudentAndSubjectAsync(int studentId, int subjectId, CancellationToken ct = default);
        Task AddAsync(Submission submission, CancellationToken ct = default);
        Task UpdateAsync(Submission submission, CancellationToken ct = default);
    }
}
