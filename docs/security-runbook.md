# Security runbook — manual steps

These cannot be done from the codebase. Do them in this order.

## 1. Rotate every secret that was committed (P0)

The following values were in git (and in the Docker image pushed to Docker Hub). Treat them as public.

| Secret | Where to rotate |
| --- | --- |
| JWT signing key (`AppSettings:Token`) | generate a new 64+ char key; all users must log in again |
| AWS access key for SNS | IAM → user `sns-publisher` → deactivate + delete old key, create new one (policy: `sns:Publish` only) |
| Google OAuth client secrets (dev + prod clients) | Google Cloud console → Credentials → OAuth client → reset secret |
| SMTP password (`smtp.titan.email`) | mail provider admin |
| Production MySQL passwords (`191.96.63.52`, both `u216915831_*` databases) | hosting panel |
| SQL Server `sa` password (dev) | change and stop using `sa` for the app; create a dedicated login |
| Docker Hub image `jaberfeka/voltax-api` | delete old tags or make the repository private; old layers contain `appsettings.production.json` |

Then put the new values in `/etc/eveon/api.env` (production) and `dotnet user-secrets` (dev). See `docs/configuration.md`.

## 2. Restrict Google browser keys

Google Cloud → Credentials:
- Maps key (`environment.*.ts` `googleMapsApiKey`): Application restriction = HTTP referrers (`https://app.eveon.ma/*`, `http://localhost:4200/*`); API restriction = Maps JavaScript API.
- Geocoding key (`googleGeocodingApiKey`): rotate it, same referrer restriction, API restriction = Geocoding API.

## 3. Purge secrets from git history (after step 1)

This rewrites history and needs a force push; every collaborator must re-clone afterwards.

```bash
pip install git-filter-repo
git clone --mirror https://github.com/GeekChanaa/VoltaX.git voltax-mirror && cd voltax-mirror
# replacements.txt: one line per leaked value, e.g.  OLDVALUE==>REMOVED
git filter-repo --replace-text ../replacements.txt
git push --force --mirror
```

Also ask GitHub support to purge cached views if the repo was ever public.

## 4. HTTPS

1. Create DNS `A` records: `api.eveon.ma` → API server, `app.eveon.ma` → dashboard host.
2. Install Caddy on the API server, copy `VoltaXApi/deploy/Caddyfile`, reload.
3. Set `AllowedHosts`, `Cors__AllowedOrigins__0`, `ReverseProxy__KnownProxies__0` in `api.env`.
4. Reconfigure chargers to `wss://api.eveon.ma/ocpp/<chargePointId>` (OCPP security profile 2: TLS + Basic auth).
   Profile 1 over plain `ws://` is refused in production unless `Ocpp__AllowInsecureProfile1=true`; set profile 2 per charger once it uses `wss://`.
5. Close port 80/8080 to the container from the outside (only Caddy on 80/443).

## 5. Charger passwords

Charger Basic auth is now enforced. Check every charger has a password set in the dashboard before deploying,
or chargers without one will be refused (see the OCPP section in the release notes).

## 5b. Charger certificates (OCPP security profile 3)

1. Production charger CA: create it offline (ECDSA P-256, 10 years, `basicConstraints=critical,CA:true,pathlen:0`,
   `keyUsage=critical,keyCertSign,cRLSign`), e.g.
   `openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:P-256 -nodes -days 3650 -subj "/CN=EVEON Charger CA/O=EVEON" -addext "basicConstraints=critical,CA:true,pathlen:0" -addext "keyUsage=critical,keyCertSign,cRLSign" -keyout charger-ca.key -out charger-ca.pem`.
   Keep an offline backup of the key. Give it to the API as `Pki__CaCertificatePem` / `Pki__CaPrivateKeyPem`
   (base64 of each PEM) in `api.env`, or as files (`Pki__CaCertificatePath`/`Pki__CaKeyPath`, `chmod 600`).
   Never enable `Pki__AutoCreateDevCa` in production. Check the *Charger PKI* admin page shows the CA.
2. Caddy: copy `VoltaXApi/deploy/Caddyfile.mtls` (separate `ocpp.` host, `client_auth verify_if_given` with the
   CA PEM), set `Ocpp__Tls__ClientCertHeader=X-Client-Cert` and make sure `ReverseProxy__KnownProxies__0` is the
   address Caddy connects from. Without a proxy, use `Ocpp__Tls__Enabled` + `Ocpp__Tls__CertificatePath` instead.
3. Per charger (*Charge point > Certificates*): install the root of the CSMS **server** certificate as
   CSMSRootCertificate (ISRG Root X1 for Let's Encrypt; the charger CA only when Kestrel serves a certificate it
   issued), *Request certificate renewal* (the charger sends a CSR, the CSMS signs it), wait for an *Active*
   certificate, then *Set security profile 3*. Most chargers keep `SecurityCtrlr.SecurityProfile` read-only:
   follow the hints returned (SetNetworkProfile with securityProfile 3 + NetworkConfigurationPriority + Reset)
   and force the CSMS-side profile only once the charger has switched — a profile 3 charger without a valid
   certificate is refused.
4. Compromised charger: *Revoke* its certificate (refused at the next handshake), set a new profile or
   renew. Revocation is CSMS-side only (no CRL/OCSP is published).
5. Expiry: the daily monitor requests renewal 30 days ahead (`Pki__RenewBeforeDays`) and notifies the
   dashboard; it also warns 90 days before the CA expires. A CA rollover means installing the new CA in Caddy's
   trust pool next to the old one, then renewing every charger certificate.

## 6. External penetration test

After the code changes are deployed to a staging environment, book an external pentest
(API, dashboard, OCPP endpoint, payment flow). Fix high/critical findings and retest before launch.
