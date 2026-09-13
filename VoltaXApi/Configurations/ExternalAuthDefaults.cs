namespace VoltaXApi.Configurations
{
    public static class ExternalAuthDefaults
    {
        /// <summary>
        /// Short lived cookie scheme that only carries the identity between the Google
        /// redirect and our callback. The API itself stays stateless / JWT based.
        /// </summary>
        public const string ExternalCookieScheme = "External";

        /// <summary>Key used to carry the account being linked through the OAuth state.</summary>
        public const string LinkUserIdItem = "voltax:linkUserId";

        /// <summary>Key used to carry the portal (customer / partner) through the OAuth state.</summary>
        public const string PortalItem = "voltax:portal";

        public const string CustomerPortal = "customer";
        public const string PartnerPortal = "partner";
    }
}
