using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Extended user repository for Phase 1 subscription features.
    /// The existing IAuthService handles authentication; this adds
    /// user lookup methods needed by subscription/linkage handlers.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);
        Task<User?> GetByStudentLinkageCodeAsync(string code, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
    }
}
