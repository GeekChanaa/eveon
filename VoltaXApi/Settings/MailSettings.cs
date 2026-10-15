namespace VoltaXApi.Settings
{
    public class MailSettings
    {
        // Sender address ("From").
        public string Mail { get; set; }
        // SMTP login when it differs from the sender (Brevo: xxxx@smtp-brevo.com). Empty = Mail.
        public string? Username { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}