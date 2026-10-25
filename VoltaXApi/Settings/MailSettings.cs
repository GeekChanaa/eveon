namespace VoltaXApi.Settings
{
    public class MailSettings
    {
        // Sender address ("From").
        public string Mail { get; set; }
        // SMTP login when it differs from the sender (Brevo: xxxx@smtp-brevo.com). Empty = Mail.
        public string? Username { get; set; }
        // Name shown with the sender address; empty = "EVEON".
        public string? DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        // Brevo API key (xkeysib-...): when set, mail goes through Brevo's HTTPS API instead of SMTP (Host, Port,
        // Username and Password are then unused). For hosts that block outbound SMTP, such as Railway's Hobby plan.
        public string? BrevoApiKey { get; set; }
    }
}