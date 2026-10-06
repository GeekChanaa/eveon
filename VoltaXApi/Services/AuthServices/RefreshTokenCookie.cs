using VoltaXApi.Dtos;

namespace VoltaXApi.Services;

/// <summary>
/// Browsers keep the refresh token in an HttpOnly cookie scoped to /api/auth (refresh and
/// logout), out of reach of scripts. Mobile clients cannot use cookies and keep sending it
/// in the body, so the API accepts either. A browser that sends "X-Token-Transport: cookie"
/// does not get the raw token in the JSON body at all.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "eveon_rt";
    public const string TransportHeader = "X-Token-Transport";

    public static LoginResultDto Apply(HttpContext context, LoginResultDto result)
    {
        if (result == null || string.IsNullOrEmpty(result.RefreshToken))
            return result;

        context.Response.Cookies.Append(Name, result.RefreshToken, Options(context, result.RefreshTokenExpiresAt));

        if (string.Equals(context.Request.Headers[TransportHeader], "cookie", StringComparison.OrdinalIgnoreCase))
            result.RefreshToken = null;

        return result;
    }

    /// <summary>Body token first (mobile), then the cookie (browser).</summary>
    public static string? Read(HttpContext context, string? bodyToken) =>
        !string.IsNullOrWhiteSpace(bodyToken) ? bodyToken : context.Request.Cookies[Name];

    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, Options(context, null));

    private static CookieOptions Options(HttpContext context, DateTime? expires)
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        return new CookieOptions
        {
            HttpOnly = true,
            // Only disable for a plain HTTP test deployment: a non-secure cookie can be sniffed.
            Secure = config.GetValue("Auth:RefreshCookie:Secure", true),
            SameSite = config.GetValue("Auth:RefreshCookie:SameSite", SameSiteMode.Strict),
            Path = context.Request.PathBase.Add("/api/auth").Value,
            Expires = expires,
            IsEssential = true
        };
    }
}
