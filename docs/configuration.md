# Configuration and secrets

`appsettings.json` and `appsettings.production.json` hold **no secrets**. The API refuses to start
when a required secret is missing (`Configurations/SecurityConfiguration.cs`).

## Local development

Secrets live in .NET user-secrets (stored in your user profile, outside the repo):

```bash
cd VoltaXApi
dotnet user-secrets list
dotnet user-secrets set "AppSettings:Token" "<64+ random characters>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=VoltaX;User=sa;Password=...;TrustServerCertificate=true"
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
dotnet user-secrets set "MailSettings:Password" "..."
dotnet user-secrets set "AwsSns:AccessKey" "..."
dotnet user-secrets set "AwsSns:SecretKey" "..."
```

User-secrets are only loaded when `ASPNETCORE_ENVIRONMENT=Development`.

## Production

All values are environment variables (`:` becomes `__`). Template: `VoltaXApi/deploy/api.env.example`.
On the server they live in `/etc/eveon/api.env` (`chmod 600`), passed with `docker run --env-file`.

| Key | Required | Notes |
| --- | --- | --- |
| `AppSettings__Token` | yes | JWT signing key, ≥ 64 chars (`openssl rand -base64 64`) |
| `AppSettings__Issuer` / `AppSettings__Audience` | no | default `eveon-api` / `eveon-clients` |
| `ConnectionStrings__DefaultConnection` | yes | SQL Server connection string |
| `AllowedHosts` | yes (prod) | API host names, e.g. `api.eveon.ma` |
| `Cors__AllowedOrigins__0..n` | yes (prod) | dashboard origins, e.g. `https://app.eveon.ma` |
| `ReverseProxy__KnownProxies__0..n` | behind a proxy | IP of the proxy allowed to send `X-Forwarded-*` |
| `Security__HttpsRedirection` | no | default `true` outside development |
| `RateLimiting__AuthStrictPerMinute` / `PublicPerMinute` / `GlobalPerMinute` | no | defaults 5 / 60 / 600 |
| `Authentication__Google__ClientId` / `ClientSecret` | for Google login | |
| `MailSettings__Mail` / `MailSettings__Password` | for email | |
| `AwsSns__AccessKey` / `AwsSns__SecretKey` | for SMS | use a least-privilege IAM user (sns:Publish only) |

## HTTPS

The container listens on `127.0.0.1:8080`; `VoltaXApi/deploy/Caddyfile` terminates TLS with an
automatic Let's Encrypt certificate and proxies REST, SignalR and the OCPP websocket (`wss://`).
The dashboard `.htaccess` forces HTTPS and sets HSTS/CSP; its `environment.prod.ts` points to
`https://api.eveon.ma` — update both if the domain differs.

## OCPP security profiles and charger PKI

Each charge point has a **security profile** (`ChargePoints.SecurityProfile`, changed only from the
charge point's *Certificates* tab / `POST /ocpp/Security/SetSecurityProfile/{id}`), enforced at the
WebSocket handshake:

| Profile | Transport | Charger authentication |
| --- | --- | --- |
| 1 (default, existing chargers) | `wss://`, or `ws://` only when `Ocpp__AllowInsecureProfile1=true` | Basic auth (username = charge point id) |
| 2 | `wss://` required | Basic auth |
| 3 | `wss://` required | TLS client certificate issued by the charger CA, CN = charge point id, not revoked; Basic auth is not used |

"TLS" means `Request.IsHttps`: Kestrel's own TLS, or `X-Forwarded-Proto: https` from a proxy listed in
`ReverseProxy:KnownProxies`. If profile 1/2 chargers are refused with `InsecureTransportNotAllowed` /
`TlsRequired` behind Caddy, the proxy address is missing from `ReverseProxy__KnownProxies`.
Chargers that only had a pinned `ClientCertThumb` (no password) were migrated to profile 3; a pinned
thumbprint (SHA-1 or SHA-256) is still accepted for them.

| Key | Default | Notes |
| --- | --- | --- |
| `Ocpp__AllowInsecureProfile1` | `true` in Development, else `false` | profile 1 over plain `ws://` |
| `Ocpp__Tls__ClientCertHeader` | unset | header in which the TLS proxy forwards the client certificate (`X-Client-Cert` with `deploy/Caddyfile.mtls`). Read only from `ReverseProxy:KnownProxies` and loopback, removed from every other request |
| `Ocpp__Tls__Enabled` | `false` | Kestrel terminates TLS itself: every HTTPS endpoint requests (optional) client certificates. Declare the endpoint with `ASPNETCORE_URLS=https://+:8443` (or `Kestrel:Endpoints`) |
| `Ocpp__Tls__CertificatePath` / `CertificatePassword` / `KeyPath` | unset | Kestrel server certificate: `.pfx` + password, or PEM certificate + PEM key |
| `Pki__CaCertificatePem` / `Pki__CaPrivateKeyPem` | unset | charger CA as PEM or base64 of the PEM (secrets: env / user-secrets only) |
| `Pki__CaCertificatePath` / `Pki__CaKeyPath` / `Pki__CaKeyPassword` | unset | charger CA from files (PEM cert + PKCS#8 key, optionally encrypted; or a `.pfx` in `CaCertificatePath`) |
| `Pki__AutoCreateDevCa` | `true` in Development, else `false` | create once a self-signed ECDSA P-256 CA stored in `PkiCertificateAuthorities`, key encrypted with Data Protection. Never for production |
| `Pki__ChargerCertificateDays` | `365` | validity of issued charger certificates (capped by the CA's) |
| `Pki__RenewBeforeDays` | `30` | the daily monitor asks connected chargers to renew (TriggerMessage SignChargingStationCertificate) this long before expiry |

Certificate flow (OCPP 2.0.1 A02/A03): the charger sends `SignCertificate` with a CSR (ECDSA P-256/P-384 or
RSA ≥ 2048, subject `CN=<chargePointId>`, optional `O`/`OU`/`C`); the CSMS answers Accepted/Rejected at once,
then signs it (KeyUsage digitalSignature + keyAgreement, EKU clientAuth) and sends `CertificateSigned`
(leaf + CA PEM). When the charger accepts it, it becomes the *Active* certificate and the previous one
*Replaced* (still trusted until it expires). V2G CSRs are rejected.

Not supported yet: `GetCertificateStatus` always answers `Failed` (`NoOCSP`, the CSMS has no OCSP client),
`Get15118EVCertificate` always answers `Failed` (ISO 15118 Plug & Charge), and certificate commands to
OCPP 1.6 chargers (the 1.6 security extension) are refused with HTTP 400.
