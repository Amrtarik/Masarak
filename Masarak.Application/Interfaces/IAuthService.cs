using Masarak.Application.DTOs;

namespace Masarak.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<MessageResponse> LogoutAsync(string refreshToken);
        Task<MessageResponse> RevokeTokenAsync(string refreshToken, int requestingUserId, string requestingUserRole);
        Task<MessageResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<UserInfoDto?> GetCurrentUserAsync(int userId);
    }
}
