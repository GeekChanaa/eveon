using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Pki;
using VoltaXApi.OCPP.Services;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

string Csr(AsymmetricAlgorithm key, string subject)
{
    var request = key switch
    {
        ECDsa ec => new CertificateRequest(subject, ec, HashAlgorithmName.SHA256),
        RSA rsa => new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
        _ => throw new ArgumentException()
    };
    return request.CreateSigningRequestPem();
}

var now = DateTimeOffset.UtcNow;
using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);

// ---------------------------------------------------------------- CSR validation
var good = ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP1"), "CP1");
Check(good.IsValid && good.Request != null, "P-256 CSR with CN = identity accepted");
Check(ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP1, O=EVEON, OU=Chargers, C=MA"), "CP1").IsValid, "optional O/OU/C accepted");
Check(ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP2"), "CP1") is { IsValid: false, Error: var e1 } && e1!.Contains("CN"), "CN of another charger rejected");
Check(!ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=cp1"), "CP1").IsValid, "CN comparison is case-sensitive");
Check(!ChargerPkiCrypto.ValidateCsr(Csr(p256, "O=EVEON"), "CP1").IsValid, "CSR without CN rejected");
Check(!ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP1, CN=CP1"), "CP1").IsValid, "two CNs rejected");
Check(!ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP1, L=Rabat"), "CP1").IsValid, "unexpected subject attribute rejected");
using (var rsa1024 = RSA.Create(1024))
    Check(ChargerPkiCrypto.ValidateCsr(Csr(rsa1024, "CN=CP1"), "CP1") is { IsValid: false, Error: var e2 } && e2!.Contains("2048"), "RSA 1024 rejected (weak key)");
using (var rsa2048 = RSA.Create(2048))
    Check(ChargerPkiCrypto.ValidateCsr(Csr(rsa2048, "CN=CP1"), "CP1").IsValid, "RSA 2048 accepted");
using (var p521 = ECDsa.Create(ECCurve.NamedCurves.nistP521))
    Check(!ChargerPkiCrypto.ValidateCsr(Csr(p521, "CN=CP1"), "CP1").IsValid, "ECDSA P-521 rejected");

var csrDer = new CertificateRequest("CN=CP1", p256, HashAlgorithmName.SHA256).CreateSigningRequest();
var tampered = (byte[])csrDer.Clone();
tampered[^1] ^= 0x01; // inside the signature
Check(ChargerPkiCrypto.ValidateCsr(PemEncoding.WriteString("CERTIFICATE REQUEST", tampered), "CP1") is { IsValid: false, Error: var e3 } && e3!.Contains("signature"), "bad signature rejected");
var subjectTampered = (byte[])csrDer.Clone();
var cpIndex = subjectTampered.AsSpan().IndexOf("CP1"u8);
subjectTampered[cpIndex + 2] = (byte)'2'; // CN=CP2 under CP1's signature
Check(ChargerPkiCrypto.ValidateCsr(PemEncoding.WriteString("CERTIFICATE REQUEST", subjectTampered), "CP2") is { IsValid: false, Error: var e4 } && e4!.Contains("signature"), "subject altered after signing rejected (signature)");
Check(ChargerPkiCrypto.ValidateCsr(Convert.ToBase64String(csrDer), "CP1").IsValid, "bare base64 DER CSR accepted");
Check(ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP1").Replace("CERTIFICATE REQUEST", "NEW CERTIFICATE REQUEST"), "CP1").IsValid, "NEW CERTIFICATE REQUEST label accepted");
Check(!ChargerPkiCrypto.ValidateCsr("not a csr", "CP1").IsValid && !ChargerPkiCrypto.ValidateCsr("", "CP1").IsValid, "garbage / empty rejected");

// ---------------------------------------------------------------- signing
using var ca = ChargerPkiCrypto.CreateDevelopmentCa("CN=Test Charger CA", now);
Check(ca.HasPrivateKey && ca.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority, "development CA is a CA with its key");
Check(ca.GetECDsaPublicKey()!.KeySize == 256, "development CA uses ECDSA P-256");

using var leaf = ChargerPkiCrypto.IssueChargerCertificate(good.Request!, ca, 365, now);
Check(leaf.Subject == "CN=CP1" && leaf.Issuer == ca.Subject && !leaf.HasPrivateKey, "leaf keeps the CSR subject, issued by the CA");
Check(Math.Abs((leaf.NotAfter.ToUniversalTime() - now.UtcDateTime).TotalDays - 365) < 1, "validity Pki:ChargerCertificateDays (365)");
var eku = leaf.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();
Check(eku.EnhancedKeyUsages.Cast<Oid>().Select(o => o.Value).SequenceEqual(new[] { ChargerPkiCrypto.ClientAuthOid }), "EKU is clientAuth only");
var keyUsage = leaf.Extensions.OfType<X509KeyUsageExtension>().Single();
Check(keyUsage.Critical && keyUsage.KeyUsages == (X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement), "KeyUsage digitalSignature + keyAgreement (critical)");
Check(!leaf.Extensions.OfType<X509BasicConstraintsExtension>().Single().CertificateAuthority, "leaf is not a CA");
Check(leaf.GetECDsaPublicKey()!.ExportSubjectPublicKeyInfo().SequenceEqual(p256.ExportSubjectPublicKeyInfo()), "leaf carries the CSR public key");
Check(ChargerPkiCrypto.ValidateClientCertificate(leaf, ca, "CP1", DateTime.UtcNow) == ClientCertificateCheck.Valid, "issued certificate chains to the CA (client auth)");

var chainPem = ChargerPkiCrypto.ToPem(leaf) + ChargerPkiCrypto.ToPem(ca);
var chain = new X509Certificate2Collection();
chain.ImportFromPem(chainPem);
Check(chain.Count == 2 && chain[0].Thumbprint == leaf.Thumbprint && chain[1].Thumbprint == ca.Thumbprint, "CertificateSigned chain is leaf + CA PEM");
using (var x509chain = new X509Chain())
{
    x509chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    x509chain.ChainPolicy.CustomTrustStore.Add(chain[1]);
    x509chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    x509chain.ChainPolicy.ApplicationPolicy.Add(new Oid(ChargerPkiCrypto.ClientAuthOid));
    Check(x509chain.Build(chain[0]), "the delivered chain validates with clientAuth policy (independent X509Chain)");
}

Check(ChargerPkiCrypto.ValidateClientCertificate(leaf, ca, "CP2", DateTime.UtcNow) == ClientCertificateCheck.SubjectMismatch, "certificate of CP1 refused for CP2");
Check(ChargerPkiCrypto.ValidateClientCertificate(leaf, ca, "CP1", DateTime.UtcNow.AddDays(400)) == ClientCertificateCheck.Expired, "expired certificate refused");
using var otherCa = ChargerPkiCrypto.CreateDevelopmentCa("CN=Other CA", now);
Check(ChargerPkiCrypto.ValidateClientCertificate(leaf, otherCa, "CP1", DateTime.UtcNow) == ClientCertificateCheck.UntrustedChain, "certificate of another CA refused");
using (var selfSigned = new CertificateRequest("CN=CP1", p256, HashAlgorithmName.SHA256).CreateSelfSigned(now.AddMinutes(-5), now.AddDays(10)))
    Check(ChargerPkiCrypto.ValidateClientCertificate(selfSigned, ca, "CP1", DateTime.UtcNow) == ClientCertificateCheck.UntrustedChain, "self-signed certificate with the right CN refused");
using (var serverKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
{
    var serverRequest = new CertificateRequest("CN=CP1", serverKey, HashAlgorithmName.SHA256);
    serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
    using var serverCert = serverRequest.Create(ca, now.AddMinutes(-5), now.AddDays(10), new byte[] { 1, 2, 3, 4 });
    Check(ChargerPkiCrypto.ValidateClientCertificate(serverCert, ca, "CP1", DateTime.UtcNow) == ClientCertificateCheck.NotForClientAuth, "serverAuth-only certificate refused");
}
using (var shortKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
using (var shortCa = CreateCa(new CertificateRequest("CN=Short CA", shortKey, HashAlgorithmName.SHA256), now, 30))
using (var capped = ChargerPkiCrypto.IssueChargerCertificate(good.Request!, shortCa, 365, now))
    Check(capped.NotAfter <= shortCa.NotAfter, "validity capped by the CA's");

var hash = ChargerPkiCrypto.ComputeHashData(ca, ca);
Check(hash.IssuerNameHash.Length == 64 && hash.IssuerKeyHash.Length == 64, "SHA-256 hash data of the CA");
Check(ChargerPkiCrypto.HashDataMatches(hash, "SHA256", hash.IssuerNameHash.ToUpperInvariant(), hash.IssuerKeyHash, "00" + hash.SerialNumber.ToUpperInvariant()),
    "hash data match ignores case and leading zeros of the serial");
Check(!ChargerPkiCrypto.HashDataMatches(hash, "SHA256", hash.IssuerNameHash, hash.IssuerKeyHash, "abcdef"), "different serial does not match");

// ---------------------------------------------------------------- CA loading from configuration
var keyPem = PemEncoding.WriteString("PRIVATE KEY", ca.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey());
var caConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Pki:CaCertificatePem"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(ChargerPkiCrypto.ToPem(ca))),
    ["Pki:CaPrivateKeyPem"] = keyPem
}).Build();
var emptyProvider = new ServiceCollection().BuildServiceProvider();
var configuredCa = new ChargerCertificateAuthority(caConfig, new TestEnvironment(Environments.Production), emptyProvider.GetRequiredService<IServiceScopeFactory>(),
    new EphemeralDataProtectionProvider(), NullLogger<ChargerCertificateAuthority>.Instance);
var loaded = await configuredCa.GetCaAsync();
Check(loaded != null && loaded.HasPrivateKey && loaded.Thumbprint == ca.Thumbprint, "CA loaded from Pki:CaCertificatePem (base64) + Pki:CaPrivateKeyPem (PEM)");
var info = await configuredCa.DescribeAsync();
Check(info.Configured && info.KeyAlgorithm == "ECDSA" && info.Source.Contains("Configuration"), "CA description");
var noCa = new ChargerCertificateAuthority(new ConfigurationBuilder().Build(), new TestEnvironment(Environments.Production), emptyProvider.GetRequiredService<IServiceScopeFactory>(),
    new EphemeralDataProtectionProvider(), NullLogger<ChargerCertificateAuthority>.Instance);
Check(await noCa.GetCaAsync() == null && !(await noCa.DescribeAsync()).Configured, "no CA outside Development unless configured (AutoCreateDevCa default off)");
var caDir = Path.Combine(Path.GetTempPath(), "voltax-pki-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(caDir);
try
{
    File.WriteAllText(Path.Combine(caDir, "ca.pem"), ChargerPkiCrypto.ToPem(ca));
    File.WriteAllText(Path.Combine(caDir, "ca.key"), keyPem);
    var fileCa = new ChargerCertificateAuthority(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Pki:CaCertificatePath"] = Path.Combine(caDir, "ca.pem"),
        ["Pki:CaKeyPath"] = Path.Combine(caDir, "ca.key")
    }).Build(), new TestEnvironment(Environments.Production), emptyProvider.GetRequiredService<IServiceScopeFactory>(),
        new EphemeralDataProtectionProvider(), NullLogger<ChargerCertificateAuthority>.Instance);
    Check((await fileCa.GetCaAsync())?.Thumbprint == ca.Thumbprint, "CA loaded from Pki:CaCertificatePath + Pki:CaKeyPath");
    using var signedWithFileCa = ChargerPkiCrypto.IssueChargerCertificate(good.Request!, (await fileCa.GetCaAsync())!, 30, now);
    Check(ChargerPkiCrypto.ValidateClientCertificate(signedWithFileCa, ca, "CP1", DateTime.UtcNow) == ClientCertificateCheck.Valid, "file-loaded CA key signs");
}
finally
{
    Directory.Delete(caDir, true);
}

// ---------------------------------------------------------------- security profile enforcement
var validator = new TestValidator(ca);
const string Password = "correct-horse-battery-staple";
ChargePoint Point(int profile, bool password = true, string? pin = null) => new()
{
    ChargePointId = "CP1", SecurityProfile = profile,
    Password = password ? ChargePointPasswordHasher.Hash(Password) : null, ClientCertThumb = pin ?? ""
};
DefaultHttpContext Context(bool tls, string? basicPassword = null, X509Certificate2? cert = null, bool forwarded = false)
{
    var context = new DefaultHttpContext();
    context.Request.Scheme = tls ? "https" : "http";
    if (basicPassword != null)
        context.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("CP1:" + basicPassword));
    if (cert != null && !forwarded) context.Connection.ClientCertificate = cert;
    if (cert != null && forwarded) context.Items[OcppClientCertificates.ForwardedItemKey] = cert;
    return context;
}
ChargePointAuthResult Auth(ChargePoint point, HttpContext context, bool allowInsecure = false) =>
    new AuthenticationService(new OcppTransportSettings { AllowInsecureProfile1 = allowInsecure }, validator)
        .AuthenticateChargePoint(context, point, "CP1", out _);

using var otherLeaf = ChargerPkiCrypto.IssueChargerCertificate(ChargerPkiCrypto.ValidateCsr(Csr(p256, "CN=CP9"), "CP9").Request!, ca, 365, now);
using var foreignLeaf = ChargerPkiCrypto.IssueChargerCertificate(good.Request!, otherCa, 365, now);
using var revokedLeaf = ChargerPkiCrypto.IssueChargerCertificate(good.Request!, ca, 365, now);
validator.Revoked.Add(ChargerPkiCrypto.Sha256Thumbprint(revokedLeaf));

var matrix = new (string Name, ChargePoint Point, HttpContext Context, bool AllowInsecure, ChargePointAuthResult Expected)[]
{
    ("P1 ws, insecure profile 1 not allowed", Point(1), Context(false, Password), false, ChargePointAuthResult.InsecureTransportNotAllowed),
    ("P1 ws, Ocpp:AllowInsecureProfile1", Point(1), Context(false, Password), true, ChargePointAuthResult.Success),
    ("P1 wss + Basic", Point(1), Context(true, Password), false, ChargePointAuthResult.Success),
    ("P1 wss wrong password", Point(1), Context(true, "wrong-password-0000"), false, ChargePointAuthResult.InvalidPassword),
    ("P1 wss no password configured", Point(1, password: false), Context(true, Password), false, ChargePointAuthResult.NoCredentialsConfigured),
    ("P2 ws refused even with AllowInsecureProfile1", Point(2), Context(false, Password), true, ChargePointAuthResult.TlsRequired),
    ("P2 wss + Basic", Point(2), Context(true, Password), false, ChargePointAuthResult.Success),
    ("P2 wss without Authorization", Point(2), Context(true), false, ChargePointAuthResult.MissingOrMalformedHeader),
    ("P2 wss client certificate is not a substitute for Basic", Point(2), Context(true, cert: leaf), false, ChargePointAuthResult.MissingOrMalformedHeader),
    ("P3 ws refused", Point(3), Context(false, cert: leaf), true, ChargePointAuthResult.TlsRequired),
    ("P3 wss Basic only (no certificate)", Point(3), Context(true, Password), false, ChargePointAuthResult.ClientCertificateRequired),
    ("P3 wss valid certificate", Point(3, password: false), Context(true, cert: leaf), false, ChargePointAuthResult.Success),
    ("P3 wss valid certificate, Basic ignored", Point(3), Context(true, "wrong-password-0000", leaf), false, ChargePointAuthResult.Success),
    ("P3 wss certificate of another charger", Point(3), Context(true, cert: otherLeaf), false, ChargePointAuthResult.InvalidCertificate),
    ("P3 wss certificate of a foreign CA", Point(3), Context(true, cert: foreignLeaf), false, ChargePointAuthResult.InvalidCertificate),
    ("P3 wss revoked certificate", Point(3), Context(true, cert: revokedLeaf), false, ChargePointAuthResult.InvalidCertificate),
    ("P3 certificate forwarded by a trusted proxy", Point(3), Context(false, cert: leaf, forwarded: true), false, ChargePointAuthResult.Success),
    ("legacy pinned thumbprint without password", Point(1, password: false, pin: foreignLeaf.Thumbprint), Context(true, cert: foreignLeaf), false, ChargePointAuthResult.Success),
    ("legacy pinned thumbprint, other certificate", Point(1, password: false, pin: leaf.Thumbprint), Context(true, cert: otherLeaf), false, ChargePointAuthResult.InvalidCertificate),
};
foreach (var (name, point, context, allowInsecure, expected) in matrix)
{
    var actual = Auth(point, context, allowInsecure);
    Check(actual == expected, $"profile matrix: {name} -> {expected}" + (actual == expected ? "" : $" (got {actual})"));
}

Check(OcppTransportSettings.From(new ConfigurationBuilder().Build(), new TestEnvironment(Environments.Development)).AllowInsecureProfile1, "AllowInsecureProfile1 defaults to true in Development");
Check(!OcppTransportSettings.From(new ConfigurationBuilder().Build(), new TestEnvironment(Environments.Production)).AllowInsecureProfile1, "AllowInsecureProfile1 defaults to false in Production");

// ---------------------------------------------------------------- forwarded client certificate (proxy trust)
var proxyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Ocpp:Tls:ClientCertHeader"] = "X-Client-Cert",
    ["ReverseProxy:KnownProxies:0"] = "172.17.0.1"
}).Build();
var transport = OcppTransportSettings.From(proxyConfig, new TestEnvironment(Environments.Production));
HttpContext Forwarded(string remoteIp, string? header, string path = "/OCPP/CP1")
{
    var context = new DefaultHttpContext();
    context.Request.Path = path;
    context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
    if (header != null) context.Request.Headers["X-Client-Cert"] = header;
    OcppClientCertificates.CaptureForwarded(context, transport, NullLogger.Instance);
    return context;
}
var derHeader = Convert.ToBase64String(leaf.RawData);
var fromProxy = Forwarded("172.17.0.1", derHeader);
Check((OcppClientCertificates.Resolve(fromProxy) as X509Certificate2)?.Thumbprint == leaf.Thumbprint && !fromProxy.Request.Headers.ContainsKey("X-Client-Cert"),
    "certificate header from ReverseProxy:KnownProxies accepted (Caddy base64 DER) and removed");
Check(OcppClientCertificates.Resolve(Forwarded("::ffff:172.17.0.1", derHeader)) != null, "IPv4-mapped proxy address accepted");
Check(OcppClientCertificates.Resolve(Forwarded("127.0.0.1", derHeader)) != null, "loopback proxy accepted");
var spoofed = Forwarded("203.0.113.9", derHeader);
Check(OcppClientCertificates.Resolve(spoofed) == null && !spoofed.Request.Headers.ContainsKey("X-Client-Cert"), "certificate header from an unknown address ignored and removed");
Check(Auth(Point(3), Forwarded("203.0.113.9", derHeader)) == ChargePointAuthResult.TlsRequired
      && new AuthenticationService(new OcppTransportSettings(), validator).AuthenticateChargePoint(WithHttps(Forwarded("203.0.113.9", derHeader)), Point(3), "CP1", out _) == ChargePointAuthResult.ClientCertificateRequired,
    "spoofed header gives no certificate to a profile 3 handshake");
Check(OcppClientCertificates.Resolve(Forwarded("172.17.0.1", WebUtility.UrlEncode(ChargerPkiCrypto.ToPem(leaf)))) != null, "URL-encoded PEM (nginx) accepted");
Check(OcppClientCertificates.Resolve(Forwarded("172.17.0.1", ChargerPkiCrypto.ToPem(leaf).Replace("\n", " "))) != null, "PEM with spaces accepted");
Check(OcppClientCertificates.Resolve(Forwarded("172.17.0.1", "garbage")) == null, "unreadable header ignored");
Check(OcppClientCertificates.Resolve(Forwarded("172.17.0.1", "")) == null, "empty header (no client certificate at the proxy) ignored");
var restPath = Forwarded("172.17.0.1", derHeader, "/api/ChargePoint");
Check(OcppClientCertificates.Resolve(restPath) == null && !restPath.Request.Headers.ContainsKey("X-Client-Cert"), "header outside /ocpp removed, not used");
var noHeaderConfig = OcppTransportSettings.From(new ConfigurationBuilder().Build(), new TestEnvironment(Environments.Production));
var notConfigured = new DefaultHttpContext();
notConfigured.Request.Path = "/OCPP/CP1";
notConfigured.Connection.RemoteIpAddress = IPAddress.Loopback;
notConfigured.Request.Headers["X-Client-Cert"] = derHeader;
OcppClientCertificates.CaptureForwarded(notConfigured, noHeaderConfig, NullLogger.Instance);
Check(OcppClientCertificates.Resolve(notConfigured) == null, "no Ocpp:Tls:ClientCertHeader: headers never read");

// ---------------------------------------------------------------- inbound handlers
var issued = new List<(string ChargePointId, string Csr, CertificateSigningUseEnumType Type)>();
var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Critical));
services.AddSingleton(issued);
services.AddScoped<IChargerCertificateService, RecordingCertificateService>();
var provider = services.BuildServiceProvider();
var logRepo = DispatchProxy.Create<IMessageLogRepository, MessageLogProxy>();
var caForHandler = new FixedCa(ca);
var handler = new SignCertificateHandler(NullLogger<SignCertificateHandler>.Instance, logRepo, caForHandler, provider.GetRequiredService<IServiceScopeFactory>());
async Task<JsonElement> Sign(object payload, string id = "CP1")
{
    var output = new OCPPMessage();
    var error = await handler.Handle(new OCPPMessage { Action = "SignCertificate", JsonPayload = JsonSerializer.Serialize(payload) }, output, new ChargePointStatus { Id = id });
    if (error != null) throw new Exception("handler error " + error);
    return JsonDocument.Parse(output.JsonPayload!).RootElement;
}
var accepted = await Sign(new { csr = Csr(p256, "CN=CP1"), certificateType = "ChargingStationCertificate" });
Check(accepted.GetProperty("status").GetString() == "Accepted" && !accepted.TryGetProperty("statusInfo", out _), "SignCertificate with a valid CSR answered Accepted");
var rejected = await Sign(new { csr = Csr(p256, "CN=CP2") });
Check(rejected.GetProperty("status").GetString() == "Rejected" && rejected.GetProperty("statusInfo").GetProperty("reasonCode").GetString() == "InvalidCSR", "SignCertificate with a wrong CN answered Rejected/InvalidCSR");
var again = await Sign(new { csr = Csr(p256, "CN=CP1") });
Check(again.GetProperty("status").GetString() == "Rejected" && again.GetProperty("statusInfo").GetProperty("reasonCode").GetString() == "TooFrequent", "second CSR within a minute rejected (TooFrequent)");
Check((await Sign(new { csr = Csr(p256, "CN=CP7") }, "CP7")).GetProperty("status").GetString() == "Accepted", "throttle is per charger");
var v2g = await Sign(new { csr = Csr(p256, "CN=CP1"), certificateType = "V2GCertificate" });
Check(v2g.GetProperty("status").GetString() == "Rejected" && v2g.GetProperty("statusInfo").GetProperty("reasonCode").GetString() == "NotSupported", "V2G CSR rejected (Plug & Charge not supported)");
var noCaHandler = new SignCertificateHandler(NullLogger<SignCertificateHandler>.Instance, logRepo, new FixedCa(null), provider.GetRequiredService<IServiceScopeFactory>());
var noCaOutput = new OCPPMessage();
await noCaHandler.Handle(new OCPPMessage { Action = "SignCertificate", JsonPayload = JsonSerializer.Serialize(new { csr = Csr(p256, "CN=CP1") }) }, noCaOutput, new ChargePointStatus { Id = "CP1" });
Check(JsonDocument.Parse(noCaOutput.JsonPayload!).RootElement.GetProperty("statusInfo").GetProperty("reasonCode").GetString() == "NoCA", "no CA configured -> Rejected/NoCA");
Check(issued.Count == 0, "CertificateSigned is not sent while the SignCertificate CALL is open");
for (var i = 0; i < 50 && issued.Count == 0; i++) await Task.Delay(100);
for (var i = 0; i < 50 && issued.Count < 2; i++) await Task.Delay(100);
Check(issued.Count == 2 && issued.Count(x => x.ChargePointId == "CP1") == 1 && issued.All(x => x.Type == CertificateSigningUseEnumType.ChargingStationCertificate), "accepted CSRs are signed and sent in the background (once each)");

var statusOutput = new OCPPMessage();
await new GetCertificateStatusHandler(NullLogger<GetCertificateStatusHandler>.Instance, logRepo).Handle(new OCPPMessage
{
    Action = "GetCertificateStatus",
    JsonPayload = "{\"ocspRequestData\":{\"hashAlgorithm\":\"SHA256\",\"issuerNameHash\":\"a\",\"issuerKeyHash\":\"b\",\"serialNumber\":\"1\",\"responderURL\":\"http://ocsp\"}}"
}, statusOutput, new ChargePointStatus { Id = "CP1" });
var statusJson = JsonDocument.Parse(statusOutput.JsonPayload!).RootElement;
Check(statusJson.GetProperty("status").GetString() == "Failed" && statusJson.GetProperty("statusInfo").GetProperty("reasonCode").GetString() == "NoOCSP", "GetCertificateStatus -> Failed/NoOCSP");
var evOutput = new OCPPMessage();
await new Get15118EVCertificateHandler(NullLogger<Get15118EVCertificateHandler>.Instance, logRepo).Handle(new OCPPMessage
{
    Action = "Get15118EVCertificate",
    JsonPayload = "{\"iso15118SchemaVersion\":\"urn:iso:15118:2:2013:MsgDef\",\"action\":\"Install\",\"exiRequest\":\"AA==\"}"
}, evOutput, new ChargePointStatus { Id = "CP1" });
var evJson = JsonDocument.Parse(evOutput.JsonPayload!).RootElement;
Check(evJson.GetProperty("status").GetString() == "Failed" && evJson.GetProperty("exiResponse").GetString() == "", "Get15118EVCertificate -> Failed with the required exiResponse");

Console.WriteLine($"All {checks} PKI checks passed.");
return 0;

static X509Certificate2 CreateCa(CertificateRequest request, DateTimeOffset now, int days)
{
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
    return request.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(days));
}

static HttpContext WithHttps(HttpContext context)
{
    context.Request.Scheme = "https";
    return context;
}

sealed class TestValidator(X509Certificate2 ca) : IChargerClientCertificateValidator
{
    public HashSet<string> Revoked { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ClientCertificateCheck Validate(X509Certificate2 certificate, string chargePointId)
    {
        var check = ChargerPkiCrypto.ValidateClientCertificate(certificate, ca, chargePointId, DateTime.UtcNow);
        return check == ClientCertificateCheck.Valid && Revoked.Contains(ChargerPkiCrypto.Sha256Thumbprint(certificate)) ? ClientCertificateCheck.Revoked : check;
    }
}

sealed class FixedCa(X509Certificate2? ca) : IChargerCertificateAuthority
{
    public Task<X509Certificate2?> GetCaAsync(CancellationToken cancellationToken = default) => Task.FromResult(ca);
    public X509Certificate2? GetCa() => ca;
    public Task<ChargerCaInfo> DescribeAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

sealed class RecordingCertificateService(List<(string, string, CertificateSigningUseEnumType)> issued) : IChargerCertificateService
{
    public Task<ChargerCertificate> IssueAndSendAsync(string chargePointId, string csr, CertificateSigningUseEnumType certificateType, CancellationToken cancellationToken = default)
    {
        lock (issued) issued.Add((chargePointId, csr, certificateType));
        return Task.FromResult(new ChargerCertificate());
    }
}

public class MessageLogProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.Name == nameof(IMessageLogRepository.SaveLogMessage) ? Task.FromResult(true) : throw new NotSupportedException(targetMethod?.Name);
}

sealed class TestEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "PkiChecks";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
