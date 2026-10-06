namespace VoltaXApi.Ocpi
{
    // Bound from the "Ocpi" configuration section.
    public class OcpiOptions
    {
        public const string SectionName = "Ocpi";
        public const string VersionNumber = "2.2.1";
        public const string Currency = "MAD";
        public const string TimeZone = "Africa/Casablanca";

        // When false every /ocpi endpoint answers 404 and the push worker stays idle.
        public bool Enabled { get; set; }
        public string CountryCode { get; set; } = "MA";
        public string PartyId { get; set; } = "EVE";
        // Public base URL of this API (e.g. https://api.eveon.ma), used in versions and endpoints.
        public string BaseUrl { get; set; } = "";
        public string BusinessName { get; set; } = "EVEON";
        public string? Website { get; set; }

        public string PublicBase => BaseUrl.TrimEnd('/');
        public string VersionsUrl => PublicBase + "/ocpi/versions";
        public string VersionDetailsUrl => PublicBase + "/ocpi/" + VersionNumber;
        public string ModuleUrl(string module) => module == "credentials"
            ? VersionDetailsUrl + "/credentials"
            : PublicBase + "/ocpi/cpo/" + VersionNumber + "/" + module;
    }
}
