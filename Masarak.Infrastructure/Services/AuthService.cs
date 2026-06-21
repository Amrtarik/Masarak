using Masarak.Infrastructure.Persistence;
using Masarak.Domain.Constants;
using Masarak.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Masarak.Infrastructure.Configurations;
using Masarak.Application.DTOs;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Masarak.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly Context _db;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthService(
            Context db,
            IJwtService jwtService,
            IPasswordService passwordService,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService)
        {
            _db = db;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
            if (role == null) return Fail($"Role '{request.Role}' does not exist.");

            var email = request.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return Fail("An account with this email already exists.");

            var user = new User
            {
                RoleId = role.RoleId, FullName = request.FullName.Trim(),
                Email = email, PasswordHash = _passwordService.HashPassword(request.Password),
                Phone = request.Phone, Country = request.Country,
                CreatedAt = DateTime.UtcNow, IsActive = true,
                EmailConfirmed = false, FailedLoginCount = 0
            };

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
            await CreateRoleProfileAsync(user, role.Name);
            return await IssueTokensAsync(user, role.Name);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.Include(u => u.Role)
                           .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                return Fail($"Account locked until {user.LockoutEnd.Value:HH:mm} UTC.");

            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                if (user != null) await RecordFailedAttemptAsync(user);
                return Fail("Invalid email or password.");
            }

            if (!user.IsActive) return Fail("Account deactivated. Contact support.");

            await ResetLockoutAsync(user);
            return await IssueTokensAsync(user, user.Role.Name);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null) return Fail("Invalid access token.");

            var jwtId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var userIdStr = principal.FindFirstValue("userid")
                         ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (jwtId == null || !int.TryParse(userIdStr, out var userId))
                return Fail("Invalid token claims.");

            var storedToken = await _db.RefreshTokens
                .Include(rt => rt.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken == null) return Fail("Refresh token not found.");

            if (storedToken.IsRevoked)
            {
                await CascadeRevokeAsync(storedToken.UserId, "Reuse of revoked token — possible theft");
                return Fail("Security alert: all sessions terminated.");
            }

            if (storedToken.IsUsed)    return Fail("Refresh token already used.");
            if (storedToken.ExpiresAt < DateTime.UtcNow) return Fail("Refresh token expired.");
            if (storedToken.JwtId != jwtId)  return Fail("Token pair mismatch.");
            if (storedToken.UserId != userId) return Fail("Token user mismatch.");

            var user = storedToken.User;
            if (!user.IsActive) return Fail("Account deactivated.");

            var newRawToken = _jwtService.GenerateRefreshToken();
            storedToken.IsUsed = true;
            storedToken.ReplacedByToken = newRawToken;

            var (accessToken, accessExpiry, newJwtId) =
                _jwtService.GenerateAccessToken(user.UserId, user.Email, user.FullName, user.Role.Name);

            var newRefreshToken = new RefreshToken
            {
                UserId = user.UserId, Token = newRawToken, JwtId = newJwtId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            await _db.RefreshTokens.AddAsync(newRefreshToken);
            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true, AccessToken = accessToken, RefreshToken = newRawToken,
                AccessTokenExpiry = accessExpiry, RefreshTokenExpiry = newRefreshToken.ExpiresAt,
                User = MapUserInfo(user, user.Role.Name)
            };
        }

        public async Task<MessageResponse> LogoutAsync(string refreshToken)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (token == null || token.IsRevoked) return Ok("Logged out.");
            token.IsRevoked = true;
            token.RevokedReason = "User logout";
            await _db.SaveChangesAsync();
            return Ok("Logged out successfully.");
        }

        public async Task<MessageResponse> RevokeTokenAsync(
            string refreshToken, int requestingUserId, string requestingUserRole)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (token == null)           return Fail2("Token not found.");
            if (token.UserId != requestingUserId && requestingUserRole != AppRoles.Admin)
                return Fail2("Unauthorized.");
            if (token.IsRevoked)         return Ok("Token already revoked.");

            token.IsRevoked = true;
            token.RevokedReason = requestingUserRole == AppRoles.Admin
                ? $"Revoked by Admin (UserId: {requestingUserId})" : "Revoked by owner";
            await _db.SaveChangesAsync();
            return Ok("Token revoked.");
        }

        public async Task<MessageResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Fail2("User not found.");
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return Fail2("Current password is incorrect.");

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            await CascadeRevokeAsync(userId, "Password changed");
            await _db.SaveChangesAsync();
            return Ok("Password changed. Please log in again on all devices.");
        }

        public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Fail2("No account found with this email address.");

            var token = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _db.SaveChangesAsync();

            // Send the token to the user's registered email address.
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, token);

            return Ok("If an account exists for that email, a password reset code has been sent.");
        }

        public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var token = request.Token.Trim().ToUpper();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null || !user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                return Fail2("Invalid or expired password reset token.");
            }

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await CascadeRevokeAsync(user.UserId, "Password reset");
            await _db.SaveChangesAsync();

            return Ok("Password has been reset successfully. You can now log in with your new password.");
        }

        public async Task<UserInfoDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _db.Users.Include(u => u.Role)
                           .FirstOrDefaultAsync(u => u.UserId == userId);
            return user == null ? null : MapUserInfo(user, user.Role.Name);
        }

        // ── Profile resolution helpers (used by feature services) ─────────────
        public async Task<Student?> GetStudentProfileAsync(int userId) =>
            await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);

        public async Task<Teacher?> GetTeacherProfileAsync(int userId) =>
            await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

        public async Task<Parent?> GetParentProfileAsync(int userId) =>
            await _db.Parents.FirstOrDefaultAsync(p => p.UserId == userId);

        // ── Private helpers ────────────────────────────────────────────────────
        private async Task<AuthResponse> IssueTokensAsync(User user, string roleName)
        {
            var (accessToken, accessExpiry, jwtId) =
                _jwtService.GenerateAccessToken(user.UserId, user.Email, user.FullName, roleName);

            var rawRefresh = _jwtService.GenerateRefreshToken();
            var rt = new RefreshToken
            {
                UserId = user.UserId, Token = rawRefresh, JwtId = jwtId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            await _db.RefreshTokens.AddAsync(rt);
            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true, AccessToken = accessToken, RefreshToken = rawRefresh,
                AccessTokenExpiry = accessExpiry, RefreshTokenExpiry = rt.ExpiresAt,
                User = MapUserInfo(user, roleName)
            };
        }

        private async Task CreateRoleProfileAsync(User user, string roleName)
        {
            switch (roleName)
            {
                case AppRoles.Student:
                    var grade = await _db.Grades.OrderBy(g => g.Level).FirstOrDefaultAsync();
                    if (grade != null)
                    {
                        await _db.Students.AddAsync(new Student
                        {
                            UserId = user.UserId, GradeId = grade.GradeId,
                            EnrollmentDate = DateTime.UtcNow.Date, AcademicStatus = "Active"
                        });
                        await _db.SaveChangesAsync();
                    }
                    break;
                case AppRoles.Teacher:
                    await _db.Teachers.AddAsync(new Teacher { UserId = user.UserId, HiringDate = DateTime.UtcNow.Date });
                    await _db.SaveChangesAsync();
                    break;
                case AppRoles.Parent:
                    await _db.Parents.AddAsync(new Parent { UserId = user.UserId });
                    await _db.SaveChangesAsync();
                    break;
            }
        }

        private async Task RecordFailedAttemptAsync(User user)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }
            await _db.SaveChangesAsync();
        }

        private async Task ResetLockoutAsync(User user)
        {
            if (user.FailedLoginCount != 0 || user.LockoutEnd.HasValue)
            {
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();
            }
        }

        private async Task CascadeRevokeAsync(int userId, string reason)
        {
            var tokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();
            foreach (var t in tokens) { t.IsRevoked = true; t.RevokedReason = reason; }
            await _db.SaveChangesAsync();
        }

        private static UserInfoDto MapUserInfo(User user, string roleName) => new()
        {
            UserId = user.UserId, FullName = user.FullName, Email = user.Email,
            Role = roleName, Phone = user.Phone, Country = user.Country,
            IsActive = user.IsActive, CreatedAt = user.CreatedAt
        };

        private static AuthResponse    Fail(string e)  => new() { Success = false, Error   = e };
        private static MessageResponse Fail2(string m) => new() { Success = false, Message = m };
        private static MessageResponse Ok(string m)    => new() { Success = true,  Message = m };
    }
}
