namespace Masarak.Infrastructure.Configurations
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = null!;
        public string WebhookSecret { get; set; } = null!;
    }
}
