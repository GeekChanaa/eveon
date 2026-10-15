namespace VoltaXApi.Services.Setup;

/// <summary>Settings of the start up check (section "FirstSetup"), see <see cref="FirstSetupJob"/>.</summary>
public sealed class FirstSetupOptions
{
    public bool Enabled { get; set; } = true;
    // When true a failed check stops the API instead of only logging an error.
    public bool FailStartupOnError { get; set; }
    public TestUsersOptions TestUsers { get; set; } = new();

    public sealed class TestUsersOptions
    {
        public bool Enabled { get; set; } = true;
        // New random passwords on every start (= every deploy). Passwords are only stored hashed,
        // so they can only be mailed when they were just generated.
        public bool RotatePasswordsOnStartup { get; set; } = true;
        public string CredentialsRecipient { get; set; } = "chanaa.projects@gmail.com";
        public string AdminEmail { get; set; } = "admin@eveon.ma";
        public string CustomerEmail { get; set; } = "customer@eveon.ma";
        public string PartnerEmail { get; set; } = "partner@eveon.ma";
    }
}
