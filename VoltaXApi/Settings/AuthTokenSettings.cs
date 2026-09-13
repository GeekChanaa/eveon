namespace VoltaXApi.Settings
{
    /// <summary>
    /// Lifetimes of the two tokens handed to a client. Bound from the "AuthTokens"
    /// configuration section, so an environment can shorten or lengthen them without
    /// a code change.
    /// </summary>
    public class AuthTokenSettings
    {
        public const string SectionName = "AuthTokens";

        /// <summary>How long an access token stays valid. Short on purpose — the refresh token carries the session.</summary>
        public int AccessTokenMinutes { get; set; } = 60;

        /// <summary>How long a refresh token stays valid when it is never used.</summary>
        public int RefreshTokenDays { get; set; } = 30;

        /// <summary>
        /// When true, rotating a refresh token issues the successor with a fresh
        /// <see cref="RefreshTokenDays"/> window, so an active user is never signed out.
        /// When false the successor inherits the expiry of the token it replaces, which
        /// caps the whole session at <see cref="RefreshTokenDays"/>.
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>How long spent / expired rows are kept before the cleanup drops them.</summary>
        public int CleanupRetentionDays { get; set; } = 7;

        public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);
        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
        public TimeSpan CleanupRetention => TimeSpan.FromDays(CleanupRetentionDays);
    }
}
