using System.Text;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Pki;
using VoltaXApi.OCPP.Services;

public enum ChargePointAuthResult
{
    Success,
    NoCredentialsConfigured,
    MissingOrMalformedHeader,
    UsernameMismatch,
    InvalidPassword,
    InvalidCertificate,
    /// <summary>Security profile 2/3 charger connected without TLS.</summary>
    TlsRequired,
    /// <summary>Security profile 1 over plain ws:// while Ocpp:AllowInsecureProfile1 is off.</summary>
    InsecureTransportNotAllowed,
    /// <summary>Security profile 3 charger presented no client certificate.</summary>
    ClientCertificateRequired
}

public class AuthenticationService
{
    private readonly OcppTransportSettings? _settings;
    private readonly IChargerClientCertificateValidator? _validator;

    /// <summary>Settings and certificate validator are resolved from the request services.</summary>
    public AuthenticationService()
    {
    }

    public AuthenticationService(OcppTransportSettings settings, IChargerClientCertificateValidator? validator)
    {
        _settings = settings;
        _validator = validator;
    }

    /// <summary>
    /// Enforces the charger's security profile (ChargePoint.SecurityProfile):
    /// 1 = Basic auth, plain ws:// only with Ocpp:AllowInsecureProfile1; 2 = TLS + Basic auth;
    /// 3 = TLS + client certificate issued by the charger CA (CN = identity, not revoked), Basic auth not used.
    /// A charger with only a pinned ClientCertThumb (no password) keeps authenticating with that certificate.
    /// upgradedHash is set when a legacy plain-text (or weaker) password matched; the caller should store it.
    /// </summary>
    public ChargePointAuthResult AuthenticateChargePoint(HttpContext context, ChargePoint chargePoint, string identity, out string? upgradedHash)
    {
        upgradedHash = null;
        var settings = _settings ?? context.RequestServices?.GetService<OcppTransportSettings>() ?? new OcppTransportSettings();
        // A certificate forwarded by a trusted proxy implies the charger's connection to the proxy was TLS.
        var tls = context.Request.IsHttps || context.Items.ContainsKey(OcppClientCertificates.ForwardedItemKey);

        switch (chargePoint.SecurityProfile)
        {
            case 3:
                return tls ? AuthenticateCertificate(context, chargePoint, identity) : ChargePointAuthResult.TlsRequired;
            case 2:
                if (!tls) return ChargePointAuthResult.TlsRequired;
                break;
            default:
                if (!tls && !settings.AllowInsecureProfile1) return ChargePointAuthResult.InsecureTransportNotAllowed;
                break;
        }

        if (!string.IsNullOrEmpty(chargePoint.Password))
            return AuthenticateBasic(context, chargePoint, identity, out upgradedHash);
        if (!string.IsNullOrWhiteSpace(chargePoint.ClientCertThumb))
            return AuthenticateCertificate(context, chargePoint, identity);
        return ChargePointAuthResult.NoCredentialsConfigured;
    }

    private static ChargePointAuthResult AuthenticateBasic(HttpContext context, ChargePoint chargePoint, string identity, out string? upgradedHash)
    {
        upgradedHash = null;
        if (!TryReadBasicCredentials(context.Request.Headers["Authorization"].ToString(), out var username, out var password))
            return ChargePointAuthResult.MissingOrMalformedHeader;

        // OCPP: the Basic username is the charging station identity from the URL.
        if (!string.Equals(username, identity, StringComparison.Ordinal))
            return ChargePointAuthResult.UsernameMismatch;

        if (!ChargePointPasswordHasher.Verify(chargePoint.Password, password, out var needsRehash))
            return ChargePointAuthResult.InvalidPassword;
        if (needsRehash) upgradedHash = ChargePointPasswordHasher.Hash(password);
        return ChargePointAuthResult.Success;
    }

    private static bool TryReadBasicCredentials(string? header, out string username, out string password)
    {
        username = password = "";
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        var encoded = header.Substring(6).Trim();
        var buffer = new byte[encoded.Length];
        if (!Convert.TryFromBase64String(encoded, buffer, out var written))
            return false;

        string decoded;
        try
        {
            decoded = new UTF8Encoding(false, true).GetString(buffer, 0, written);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        var separator = decoded.IndexOf(':');
        if (separator <= 0) return false;
        username = decoded.Substring(0, separator);
        password = decoded.Substring(separator + 1);
        return true;
    }

    private ChargePointAuthResult AuthenticateCertificate(HttpContext context, ChargePoint chargePoint, string identity)
    {
        var clientCert = OcppClientCertificates.Resolve(context);
        if (clientCert == null) return ChargePointAuthResult.ClientCertificateRequired;

        // Explicitly pinned certificate (SHA-1 thumbprint as before, or SHA-256).
        var pin = chargePoint.ClientCertThumb?.Trim();
        if (!string.IsNullOrEmpty(pin) && (clientCert.Thumbprint.Equals(pin, StringComparison.OrdinalIgnoreCase)
            || ChargerPkiCrypto.Sha256Thumbprint(clientCert).Equals(pin, StringComparison.OrdinalIgnoreCase)))
            return ChargePointAuthResult.Success;

        var validator = _validator ?? context.RequestServices?.GetService<IChargerClientCertificateValidator>();
        return validator != null && validator.Validate(clientCert, identity) == ClientCertificateCheck.Valid
            ? ChargePointAuthResult.Success
            : ChargePointAuthResult.InvalidCertificate;
    }
}
