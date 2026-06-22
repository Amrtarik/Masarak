using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for Exam CRUD operations.
    /// </summary>
    public interface IExamRepository
    {
        Task<Exam?> GetByIdWithQuestionsAsync(int examId, CancellationToken ct = default);
        Task<IEnumerable<Exam>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct = default);
        Task<IEnumerable<Exam>> GetOpenExamsForClassAsync(int classId, int subjectId, DateTime now, CancellationToken ct = default);
        Task AddAsync(Exam exam, CancellationToken ct = default);
        Task UpdateAsync(Exam exam, CancellationToken ct = default);
    }
}
