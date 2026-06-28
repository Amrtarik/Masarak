using Masarak.Domain.Enums;

namespace Masarak.Application.DTOs
{
    public record AdminUserDto(
        int UserId,
        string Email,
        string FullName,
        string Role,
        string? Country,
        bool IsActive,
        bool EmailConfirmed,
        DateTime CreatedAt
    );

    public record AdminCreateUserRequest
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string FullName { get; init; } = null!;
        public string Role { get; init; } = null!; // "Student", "Teacher", "Parent", "Admin"
        public string? Phone { get; init; }
        public string? Country { get; init; }
    }

    public record TeacherDto(
        int TeacherId,
        int UserId,
        string FullName,
        string Email,
        string? Specialization,
        DateTime HiringDate,
        string? Bio,
        bool IsActive
    );
}
