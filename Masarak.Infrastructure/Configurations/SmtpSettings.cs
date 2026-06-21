namespace Masarak.Infrastructure.Configurations
{
    public class SmtpSettings
    {
        public string Host     { get; set; } = "smtp.gmail.com";
        public int    Port     { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool   UseSsl   { get; set; } = true;
        public string FromAddress     { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = "Masarak";
    }
}
