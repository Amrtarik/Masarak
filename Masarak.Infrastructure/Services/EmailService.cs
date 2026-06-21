using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace Masarak.Infrastructure.Services
{
    /// <summary>
    /// MailKit-based SMTP email service.
    /// Credentials are read from SmtpSettings (bound from appsettings.json).
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;

        public EmailService(IOptions<SmtpSettings> smtpOptions)
        {
            _smtp = smtpOptions.Value;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromDisplayName, _smtp.FromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Masarak – Password Reset Code";

            var body = new BodyBuilder
            {
                HtmlBody = $"""
                    <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:32px;
                                border:1px solid #e5e7eb;border-radius:12px;">
                        <h2 style="color:#1e293b;">🔐 Password Reset Request</h2>
                        <p style="color:#475569;">Hi <strong>{toName}</strong>,</p>
                        <p style="color:#475569;">
                            We received a request to reset your Masarak account password.
                            Use the code below — it expires in <strong>1 hour</strong>.
                        </p>
                        <div style="background:#f1f5f9;border-radius:8px;padding:20px;
                                    text-align:center;margin:24px 0;">
                            <span style="font-size:32px;font-weight:700;letter-spacing:8px;
                                         color:#0f172a;">{resetToken}</span>
                        </div>
                        <p style="color:#94a3b8;font-size:13px;">
                            If you did not request this, please ignore this email.
                            Your password will not change.
                        </p>
                        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;" />
                        <p style="color:#cbd5e1;font-size:12px;text-align:center;">
                            © 2026 Masarak Education Platform
                        </p>
                    </div>
                    """,
                TextBody = $"Your Masarak password reset code is: {resetToken}\n\nThis code expires in 1 hour."
            };

            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtp.UserName, _smtp.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        }
    }
}
