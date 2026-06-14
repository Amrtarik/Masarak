using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    public interface IPlanRepository
    {
        Task<Plan?> GetByIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken ct = default);
    }
}
