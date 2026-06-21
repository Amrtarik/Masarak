namespace Masarak.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password-reset email containing the one-time token.
        /// </summary>
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
    }
}
