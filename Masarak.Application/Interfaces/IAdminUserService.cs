using Masarak.Application.DTOs;

namespace Masarak.Application.Interfaces
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role, CancellationToken ct = default);
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(CancellationToken ct = default);
        Task<AdminUserDto> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct = default);
        Task DeleteUserAsync(int userId, CancellationToken ct = default);
    }
}
