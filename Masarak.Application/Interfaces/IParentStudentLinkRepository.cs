using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    public interface IParentStudentLinkRepository
    {
        Task<bool> LinkExistsAsync(int parentUserId, int studentUserId, CancellationToken ct = default);
        Task AddAsync(ParentStudentLink link, CancellationToken ct = default);
        Task<IEnumerable<ParentStudentLink>> GetByParentUserIdAsync(int parentUserId, CancellationToken ct = default);
    }
}
