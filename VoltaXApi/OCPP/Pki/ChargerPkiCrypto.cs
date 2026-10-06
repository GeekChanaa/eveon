using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Pki
{
    public sealed record CsrValidationResult(bool IsValid, string? Error, CertificateRequest? Request)
    {
        public static CsrValidationResult Fail(string error) => new(false, error, null);
    }

    public enum ClientCertificateCheck
    {
        Valid,
        Expired,
        SubjectMismatch,
        UntrustedChain,
        NotForClientAuth,
        Revoked,
        NoCa
    }

    /// <summary>
    /// Stateless X.509 operations of the charger PKI: CSR checks, issuing, the development CA,
    /// client certificate validation and the OCPP certificate hash data.
    /// </summary>
    public static class ChargerPkiCrypto
    {
        public const string ClientAuthOid = "1.3.6.1.5.5.7.3.2";
        private const string EcPublicKeyOid = "1.2.840.10045.2.1";
        private const string RsaPublicKeyOid = "1.2.840.113549.1.1.1";
        private const string P256Oid = "1.2.840.10045.3.1.7";
        private const string P384Oid = "1.3.132.0.34";
        private const string CommonNameOid = "2.5.4.3";
        private const string OrganizationOid = "2.5.4.10";
        private const string OrganizationalUnitOid = "2.5.4.11";
        private const string CountryOid = "2.5.4.6";
        public const int MinRsaKeySize = 2048;

        /// <summary>
        /// Parses a PEM (or bare base64 DER) PKCS#10 request, verifies its signature, the key
        /// (ECDSA P-256/P-384 or RSA ≥ 2048) and the subject: exactly one CN equal to the charge point id;
        /// O, OU and C are optional; any other attribute is refused.
        /// </summary>
        public static CsrValidationResult ValidateCsr(string? csr, string chargePointId)
        {
            if (string.IsNullOrWhiteSpace(csr)) return CsrValidationResult.Fail("The CSR is empty.");
            if (csr.Length > 5500) return CsrValidationResult.Fail("The CSR is longer than 5500 characters.");

            CertificateRequest request;
            try
            {
                var text = csr.Trim().Replace("\\n", "\n").Replace("NEW CERTIFICATE REQUEST", "CERTIFICATE REQUEST");
                request = text.StartsWith("-----BEGIN", StringComparison.Ordinal)
                    ? CertificateRequest.LoadSigningRequestPem(text, HashAlgorithmName.SHA256)
                    : CertificateRequest.LoadSigningRequest(Convert.FromBase64String(text), HashAlgorithmName.SHA256);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
            {
                return CsrValidationResult.Fail("The CSR could not be parsed or its signature is invalid.");
            }

            var keyError = CheckPublicKey(request.PublicKey);
            if (keyError != null) return CsrValidationResult.Fail(keyError);

            var subjectError = CheckSubject(request.SubjectName, chargePointId);
            if (subjectError != null) return CsrValidationResult.Fail(subjectError);

            return new CsrValidationResult(true, null, request);
        }

        private static string? CheckPublicKey(PublicKey key)
        {
            switch (key.Oid.Value)
            {
                case EcPublicKeyOid:
                    string? curve;
                    try
                    {
                        curve = AsnDecoder.ReadObjectIdentifier(key.EncodedParameters.RawData, AsnEncodingRules.DER, out _);
                    }
                    catch (AsnContentException)
                    {
                        return "The ECDSA key does not use a named curve.";
                    }
                    return curve is P256Oid or P384Oid ? null : $"ECDSA curve {curve} is not accepted (use P-256 or P-384).";
                case RsaPublicKeyOid:
                    using (var rsa = key.GetRSAPublicKey())
                        return rsa != null && rsa.KeySize >= MinRsaKeySize ? null : $"RSA keys must be at least {MinRsaKeySize} bits.";
                default:
                    return $"Key algorithm {key.Oid.Value} is not accepted (use ECDSA P-256 or RSA ≥ {MinRsaKeySize}).";
            }
        }

        private static string? CheckSubject(X500DistinguishedName subject, string chargePointId)
        {
            var commonNames = new List<string>();
            foreach (var rdn in subject.EnumerateRelativeDistinguishedNames())
            {
                if (rdn.HasMultipleElements) return "Multi-valued subject attributes are not accepted.";
                var type = rdn.GetSingleElementType().Value;
                var value = rdn.GetSingleElementValue();
                if (type == CommonNameOid) commonNames.Add(value ?? "");
                else if (type is not (OrganizationOid or OrganizationalUnitOid or CountryOid))
                    return $"Subject attribute {type} is not accepted (only CN, O, OU and C).";
            }
            if (commonNames.Count != 1) return "The subject must contain exactly one CN.";
            return string.Equals(commonNames[0], chargePointId, StringComparison.Ordinal)
                ? null
                : "The subject CN must be the charging station identity.";
        }

        /// <summary>
        /// Signs the client certificate of a validated CSR: same subject and key, KeyUsage digitalSignature +
        /// keyAgreement, EKU clientAuth, not a CA. The validity is capped by the CA's.
        /// </summary>
        public static X509Certificate2 IssueChargerCertificate(CertificateRequest csr, X509Certificate2 ca, int validityDays, DateTimeOffset now)
        {
            if (!ca.HasPrivateKey) throw new InvalidOperationException("The charger CA has no private key.");

            var request = new CertificateRequest(csr.SubjectName, csr.PublicKey, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(ClientAuthOid) }, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(ca, true, false));

            var notBefore = now.AddMinutes(-5);
            var notAfter = now.AddDays(Math.Max(1, validityDays));
            var caNotAfter = new DateTimeOffset(ca.NotAfter.ToUniversalTime());
            if (notAfter > caNotAfter) notAfter = caNotAfter;
            if (notAfter <= notBefore) throw new InvalidOperationException("The charger CA is expired.");

            return request.Create(ca, notBefore, notAfter, NewSerialNumber());
        }

        /// <summary>Self-signed ECDSA P-256 CA for development (10 years).</summary>
        public static X509Certificate2 CreateDevelopmentCa(string subject, DateTimeOffset now)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            return request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(10));
        }

        /// <summary>
        /// Chain to <paramref name="ca"/> (only trust anchor), validity at <paramref name="utcNow"/>, clientAuth usage
        /// and CN == charge point id. Revocation is checked by the caller against the database.
        /// </summary>
        public static ClientCertificateCheck ValidateClientCertificate(X509Certificate2 certificate, X509Certificate2 ca, string chargePointId, DateTime utcNow)
        {
            if (utcNow < certificate.NotBefore.ToUniversalTime() || utcNow > certificate.NotAfter.ToUniversalTime())
                return ClientCertificateCheck.Expired;
            if (!string.Equals(GetCommonName(certificate), chargePointId, StringComparison.Ordinal))
                return ClientCertificateCheck.SubjectMismatch;

            var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
            if (eku != null && !eku.EnhancedKeyUsages.Cast<Oid>().Any(o => o.Value == ClientAuthOid))
                return ClientCertificateCheck.NotForClientAuth;

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationTime = utcNow;
            chain.ChainPolicy.VerificationTimeIgnored = false;
            if (!chain.Build(certificate)) return ClientCertificateCheck.UntrustedChain;
            var root = chain.ChainElements[^1].Certificate;
            return root.RawData.AsSpan().SequenceEqual(ca.RawData) ? ClientCertificateCheck.Valid : ClientCertificateCheck.UntrustedChain;
        }

        public static string? GetCommonName(X509Certificate2 certificate)
        {
            foreach (var rdn in certificate.SubjectName.EnumerateRelativeDistinguishedNames())
                if (!rdn.HasMultipleElements && rdn.GetSingleElementType().Value == CommonNameOid)
                    return rdn.GetSingleElementValue();
            return null;
        }

        public static string Sha256Thumbprint(X509Certificate2 certificate) =>
            Convert.ToHexString(SHA256.HashData(certificate.RawData));

        public static string ToPem(X509Certificate2 certificate) =>
            PemEncoding.WriteString("CERTIFICATE", certificate.RawData) + "\n";

        /// <summary>OCPP CertificateHashDataType of <paramref name="certificate"/> issued by <paramref name="issuer"/> (itself for a root).</summary>
        public static CertificateHashDataType ComputeHashData(X509Certificate2 certificate, X509Certificate2 issuer, HashAlgorithmEnumType algorithm = HashAlgorithmEnumType.SHA256)
        {
            return new CertificateHashDataType
            {
                HashAlgorithm = algorithm,
                IssuerNameHash = Hash(algorithm, issuer.SubjectName.RawData),
                IssuerKeyHash = Hash(algorithm, issuer.PublicKey.EncodedKeyValue.RawData),
                SerialNumber = NormalizeSerial(certificate.SerialNumber)
            };
        }

        public static bool HashDataMatches(CertificateHashDataType expected, string hashAlgorithm, string issuerNameHash, string issuerKeyHash, string serialNumber) =>
            string.Equals(expected.HashAlgorithm.ToString(), hashAlgorithm, StringComparison.OrdinalIgnoreCase)
            && string.Equals(expected.IssuerNameHash, issuerNameHash, StringComparison.OrdinalIgnoreCase)
            && string.Equals(expected.IssuerKeyHash, issuerKeyHash, StringComparison.OrdinalIgnoreCase)
            && NormalizeSerial(expected.SerialNumber) == NormalizeSerial(serialNumber);

        /// <summary>Hex serial without leading zeros, lower case (the form OCPP examples use).</summary>
        public static string NormalizeSerial(string serial)
        {
            var trimmed = (serial ?? "").Trim().Replace(":", "").TrimStart('0').ToLowerInvariant();
            return trimmed.Length == 0 ? "0" : trimmed;
        }

        private static string Hash(HashAlgorithmEnumType algorithm, byte[] data) => Convert.ToHexString(algorithm switch
        {
            HashAlgorithmEnumType.SHA384 => SHA384.HashData(data),
            HashAlgorithmEnumType.SHA512 => SHA512.HashData(data),
            _ => SHA256.HashData(data)
        }).ToLowerInvariant();

        private static byte[] NewSerialNumber()
        {
            var serial = RandomNumberGenerator.GetBytes(16);
            serial[0] &= 0x7F;
            if (serial[0] == 0) serial[0] = 0x01;
            return serial;
        }

        /// <summary>Parses one PEM certificate (or bare base64 DER).</summary>
        public static X509Certificate2 ParseCertificate(string text)
        {
            var trimmed = text.Trim();
            return trimmed.StartsWith("-----BEGIN", StringComparison.Ordinal)
                ? X509Certificate2.CreateFromPem(trimmed)
                : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(trimmed));
        }

        /// <summary>
        /// Client certificate forwarded by a TLS-terminating proxy: base64 DER (Caddy
        /// <c>{http.request.tls.client.certificate_der_base64}</c>), PEM, or URL-encoded PEM (nginx
        /// <c>$ssl_client_escaped_cert</c>). Null when empty or unreadable.
        /// </summary>
        public static X509Certificate2? ParseForwardedCertificate(string? headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue)) return null;
            var value = headerValue.Trim().Trim('"');
            if (value.StartsWith("%2D", StringComparison.OrdinalIgnoreCase) || value.Contains("%0A", StringComparison.OrdinalIgnoreCase))
                value = WebUtility.UrlDecode(value);
            try
            {
                if (value.StartsWith("-----BEGIN", StringComparison.Ordinal))
                {
                    // Some proxies replace the PEM newlines with spaces or tabs.
                    var body = value.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "");
                    var base64 = new StringBuilder(body.Length);
                    foreach (var c in body) if (!char.IsWhiteSpace(c)) base64.Append(c);
                    value = base64.ToString();
                }
                return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(value));
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }
    }
}
