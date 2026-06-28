using System.Security.Claims;
using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Enums;
using Masarak.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    /// <summary>
    /// Phase 6: Admin user management endpoints.
    /// Provides user listing, detail view, activation/deactivation, and password reset.
    /// Restricted to Admin role only.
    /// </summary>
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserRepository _adminUserRepo;
        private readonly IPasswordService _passwordService;
        private readonly IUserRepository _userRepo;

        public AdminUsersController(
            IAdminUserRepository adminUserRepo,
            IPasswordService passwordService,
            IUserRepository userRepo)
        {
            _adminUserRepo = adminUserRepo;
            _passwordService = passwordService;
            _userRepo = userRepo;
        }

        /// <summary>
        /// GET /api/admin/users — List all users with optional role filter and search.
        /// Query params: role, search, page, pageSize
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? role = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var (items, totalCount) = await _adminUserRepo.GetAllAsync(role, search, page, pageSize, ct);

            Response.Headers["X-Total-Count"] = totalCount.ToString();

            return Ok(new
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = page < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = page > 1
            });
        }

        /// <summary>
        /// GET /api/admin/users/{id} — Get detailed user information.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(int id, CancellationToken ct = default)
        {
            var user = await _adminUserRepo.GetByIdWithDetailsAsync(id, ct);
            if (user == null)
                return NotFound(new { Code = "USER_NOT_FOUND", Message = $"User with ID {id} not found." });

            var examsTaken = user.Student?.StudentExams?.Count ?? 0;
            var assignmentsSubmitted = user.Student?.Submissions?.Count ?? 0;
            var hasActiveSub = user.Subscriptions?.Any(s => s.Status == SubscriptionStatus.Active) ?? false;

            var dto = new AdminUserDetailDto(
                user.UserId,
                user.FullName,
                user.Email,
                user.Role.Name,
                user.IsActive,
                user.CreatedAt,
                hasActiveSub,
                examsTaken,
                assignmentsSubmitted,
                0m); // AttendancePercentage — requires separate query

            return Ok(dto);
        }

        /// <summary>
        /// PUT /api/admin/users/{id}/deactivate — Deactivate a user account.
        /// </summary>
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id, [FromBody] DeactivateUserRequest? request = null, CancellationToken ct = default)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound(new { Code = "USER_NOT_FOUND", Message = $"User with ID {id} not found." });

            await _adminUserRepo.DeactivateAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// PUT /api/admin/users/{id}/activate — Activate a user account.
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id, CancellationToken ct = default)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound(new { Code = "USER_NOT_FOUND", Message = $"User with ID {id} not found." });

            await _adminUserRepo.ActivateAsync(id, ct);
            return NoContent();
        }

        /// <summary>
        /// POST /api/admin/users/{id}/reset-password — Reset a user's password and return a temporary password.
        /// </summary>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, CancellationToken ct = default)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound(new { Code = "USER_NOT_FOUND", Message = $"User with ID {id} not found." });

            // Generate a temporary password
            var tempPassword = GenerateTemporaryPassword();
            user.PasswordHash = _passwordService.HashPassword(tempPassword);
            await _userRepo.UpdateAsync(user, ct);

            return Ok(new { TemporaryPassword = tempPassword });
        }

        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
            var random = System.Security.Cryptography.RandomNumberGenerator.Create();
            var password = new char[12];
            var randomBytes = new byte[12];
            random.GetBytes(randomBytes);
            for (int i = 0; i < 12; i++)
                password[i] = chars[randomBytes[i] % chars.Length];
            return new string(password);
        }
    }

    /// <summary>Request body for deactivating a user.</summary>
    public class DeactivateUserRequest
    {
        public string? Reason { get; set; }
    }
}
