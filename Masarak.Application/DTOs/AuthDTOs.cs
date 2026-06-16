using System.ComponentModel.DataAnnotations;

namespace Masarak.Application.DTOs
{
    // ─── Register ────────────────────────────────────────────────────────────────
    public class RegisterRequest
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required, MinLength(8), MaxLength(100)]
        public string Password { get; set; } = null!;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        /// <summary>Role name: Admin | Teacher | Student | Parent</summary>
        [Required]
        public string Role { get; set; } = null!;
    }

    // ─── Login ───────────────────────────────────────────────────────────────────
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }

    // ─── Refresh ─────────────────────────────────────────────────────────────────
    public class RefreshTokenRequest
    {
        [Required]
        public string AccessToken { get; set; } = null!;

        [Required]
        public string RefreshToken { get; set; } = null!;
    }

    // ─── Revoke ──────────────────────────────────────────────────────────────────
    public class RevokeTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }

    // ─── Change Password ─────────────────────────────────────────────────────────
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = null!;

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = null!;
    }

    // ─── Forgot Password ─────────────────────────────────────────────────────────
    public class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }

    // ─── Reset Password ──────────────────────────────────────────────────────────
    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = null!;

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = null!;
    }

    // ─── Responses ───────────────────────────────────────────────────────────────
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public UserInfoDto? User { get; set; }
        public string? Error { get; set; }
    }

    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }
}
