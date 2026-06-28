using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly Context _db;
        private readonly IPasswordService _passwordService;

        public AdminUserService(Context db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role, CancellationToken ct = default)
        {
            var query = _db.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role.Name == role);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto(
                    u.UserId,
                    u.Email,
                    u.FullName,
                    u.Role.Name,
                    u.Country,
                    u.IsActive,
                    u.EmailConfirmed,
                    u.CreatedAt
                ))
                .ToListAsync(ct);

            return new PagedResult<AdminUserDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(CancellationToken ct = default)
        {
            return await _db.Teachers
                .Include(t => t.User)
                .Select(t => new TeacherDto(
                    t.TeacherId,
                    t.UserId,
                    t.User.FullName,
                    t.User.Email,
                    t.Specialization,
                    t.HiringDate,
                    t.Bio,
                    t.User.IsActive
                ))
                .ToListAsync(ct);
        }

        public async Task<AdminUserDto> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct = default)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role, ct);
            if (role == null) throw new InvalidOperationException($"Role {request.Role} not found.");

            if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
                throw new InvalidOperationException("Email is already in use.");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                RoleId = role.RoleId,
                Country = request.Country,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);

            // If teacher or student, create profile
            if (role.Name == "Teacher")
            {
                _db.Teachers.Add(new Teacher { UserId = user.UserId, HiringDate = DateTime.UtcNow });
                await _db.SaveChangesAsync(ct);
            }
            else if (role.Name == "Student")
            {
                // Assign to first grade for now
                var grade = await _db.Grades.OrderBy(g => g.Order).FirstOrDefaultAsync(ct);
                if (grade != null)
                {
                    _db.Students.Add(new Student { UserId = user.UserId, GradeId = grade.GradeId, EnrollmentDate = DateTime.UtcNow, AcademicStatus = "Active" });
                    await _db.SaveChangesAsync(ct);
                }
            }

            return new AdminUserDto(
                user.UserId,
                user.Email,
                user.FullName,
                role.Name,
                user.Country,
                user.IsActive,
                user.EmailConfirmed,
                user.CreatedAt
            );
        }

        public async Task DeleteUserAsync(int userId, CancellationToken ct = default)
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null) throw new InvalidOperationException("User not found.");

            user.IsActive = false; // Soft delete
            await _db.SaveChangesAsync(ct);
        }
    }
}
