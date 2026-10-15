namespace VoltaXApi.Helpers;

/// <summary>
/// Base URLs of the browser apps, each with a trailing slash.
/// The dashboard (SpaLink) and the public site that serves the sign in, sign up, password
/// and verification pages (AuthLink) can be deployed separately; without an AuthLink both
/// are the dashboard, as before.
/// </summary>
public static class ClientLinks
{
    public static string Dashboard(this IConfiguration config) => WithSlash(config["SpaLink"]);

    public static string AuthSite(this IConfiguration config) =>
        WithSlash(string.IsNullOrWhiteSpace(config["AuthLink"]) ? config["SpaLink"] : config["AuthLink"]);

    private static string WithSlash(string? url) =>
        string.IsNullOrEmpty(url) || url.EndsWith('/') ? url ?? "" : url + "/";
}
