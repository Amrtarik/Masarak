using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for Assignment CRUD operations.
    /// </summary>
    public interface IAssignmentRepository
    {
        Task<Assignment?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<Assignment>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct = default);
        Task<IEnumerable<Assignment>> GetPublishedForClassAsync(int classId, int subjectId, CancellationToken ct = default);
        Task AddAsync(Assignment assignment, CancellationToken ct = default);
        Task UpdateAsync(Assignment assignment, CancellationToken ct = default);
    }
}
