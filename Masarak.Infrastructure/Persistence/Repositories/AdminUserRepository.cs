using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Phase 6: Admin user repository for user management operations.
    /// Provides paginated, searchable user lists and account activation/deactivation.
    /// </summary>
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly Context _context;

        public AdminUserRepository(Context context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<AdminUserDto> Items, int TotalCount)> GetAllAsync(
            string? roleFilter, string? searchTerm, int page, int pageSize, CancellationToken ct)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Subscriptions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roleFilter))
                query = query.Where(u => u.Role.Name == roleFilter);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(u =>
                    u.FullName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto(
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role.Name,
                    u.IsActive,
                    u.CreatedAt,
                    u.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active)))
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<User?> GetByIdWithDetailsAsync(int userId, CancellationToken ct)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Subscriptions)
                .Include(u => u.Student)
                    .ThenInclude(s => s!.StudentExams)
                .Include(u => u.Student)
                    .ThenInclude(s => s!.Submissions)
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);
        }

        public async Task ActivateAsync(int userId, CancellationToken ct)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            if (user != null)
            {
                user.IsActive = true;
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task DeactivateAsync(int userId, CancellationToken ct)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
