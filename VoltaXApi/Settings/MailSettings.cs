namespace VoltaXApi.Settings
{
    public class MailSettings
    {
        // Sender address ("From").
        public string Mail { get; set; }
        // SMTP login when it differs from the sender. Empty = Mail (Titan logs in with the mailbox address).
        public string? Username { get; set; }
        // Name shown with the sender address; empty = "EVEON".
        public string? DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        // Public URL of wwwroot/Assets/images/email/ (the logo in the emails), ending with '/'. Mail clients load the
        // images from there, so it must be reachable without signing in. Empty = the production API.
        public string? AssetsUrl { get; set; }
    }
}