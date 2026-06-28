using Masarak.Application.DTOs;
using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Phase 6: Admin user management repository.
    /// Provides paginated user lists with filters and account management operations.
    /// </summary>
    public interface IAdminUserRepository
    {
        /// <summary>Get paginated list of all users with optional role filter and search.</summary>
        Task<(IEnumerable<AdminUserDto> Items, int TotalCount)> GetAllAsync(
            string? roleFilter, string? searchTerm, int page, int pageSize, CancellationToken ct);

        /// <summary>Get a user with full details for admin view.</summary>
        Task<User?> GetByIdWithDetailsAsync(int userId, CancellationToken ct);

        /// <summary>Activate a user account.</summary>
        Task ActivateAsync(int userId, CancellationToken ct);

        /// <summary>Deactivate a user account.</summary>
        Task DeactivateAsync(int userId, CancellationToken ct);
    }
}
