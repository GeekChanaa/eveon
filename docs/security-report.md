# EVEON (VoltaX) security assessment

Authorized assessment of the EVEON platform (ASP.NET Core API `VoltaXApi` with an OCPP
1.6J + 2.0.1 CSMS, OCPI, wallet and card payments, JWT + refresh-cookie auth, Google
sign-in, TOTP 2FA, and the Angular `VoltaXDashboardSpa`). It covers IDOR / cross-tenant
isolation, authentication, payments, input/output handling and dependency/secret scans
(roadmap #219 and #220).

Everything was tested locally against in-process test hosts or a locally started API; no
production URL, host or credential was contacted. Secrets are referred to by file and key
name only, never by value.

## Method

- **Automated test suite** (`tests/VoltaX.Tests`, xUnit). `ApiFactory` runs the whole API
  in memory on SQLite including all middleware and global filters; `OcppTestHost` drives
  the real OCPP handlers; the existing `AuthorizationMatrixTests` call every one of the
  ~670 MVC action/route pairs as anonymous / customer / partner. The suite grew from 130
  to 171 tests. Full run: `dotnet test tests/VoltaX.Tests/VoltaX.Tests.csproj -c Release`
  (171 passed). The standalone check projects (`VoltaX.AuthChecks`, `VoltaX.AuthorizationChecks`,
  `VoltaX.OcpiChecks`, `VoltaX.PkiChecks`) were read and relied on for refresh-token
  rotation/reuse, forged-claim rejection, OCPI token auth and charger PKI.
- **Code review** of the riskiest files: `Authorization/*`, `AuthController`,
  `AuthService` (+ phone, partner, 2FA, password reset), `PaymentCallbacksController`,
  the payment services, `FileManagementService` / `ImageUploadValidator`,
  `GenericController` / `Repository` / `SafeDynamicQuery` / `EntityUpdate`, the OCPP
  WebSocket auth path, and the SignalR hubs.
- **Dependency scan**: `dotnet list package --vulnerable --include-transitive` and
  `npm audit --omit=dev` in `VoltaXDashboardSpa`.
- **Secret scan**: gitleaks 8.21.2 over all 196 commits (`--no-banner --redact`) plus the
  working tree, and a keyed regex scan of the full history diff.

## What was tested (with test references)

| Area | Tests |
| --- | --- |
| Every endpoint as anonymous / customer / partner, foreign ids | `Security/AuthorizationMatrixTests.cs` (pre-existing) |
| Cross-tenant IDOR with another tenant's **real** rows (sessions, orders, cards, session/order invoices, user list endpoints, notification settings; partner A vs B charge points and connector pricing) | `Security/CrossTenantIdorTests.cs` (new) |
| Mass assignment: nested navigation objects, client-supplied ids, generic-update deny list, Dynamic LINQ filter/sort/search injection and parameter binding | `Security/MassAssignmentTests.cs` (new) |
| JWT validation (wrong key, alg=none, wrong issuer/audience, expiry), suspension takes effect immediately, login lockout, logout clears the cookie | `Security/AuthenticationTests.cs` (new) |
| OCPP transport auth: Basic username must equal the charge point id, password verify, no-credentials refused, profile 1 insecure-transport refusal, profile 2 requires TLS, PBKDF2 salting/constant-time | `Security/ChargerAuthenticationTests.cs` (new) |
| OCPP application auth: tokens, balances, provisioning (pending charger cannot charge), refusals notify the owner | `Ocpp/AuthorizationScenarioTests.cs` (pre-existing) |
| Security headers, CORS origin restriction, refresh-cookie flags (HttpOnly/Secure/SameSite/Path) | `Security/HttpSecurityTests.cs` (new) |
| Payment callbacks: signature verification, replay/idempotency, amount tampering, order belongs to provider, wrong-provider webhook 404 | `Payments/ProviderSignatureTests.cs`, `Payments/PaymentFlowTests.cs` (pre-existing), `Payments/PaymentNotificationTests.cs` (new) |
| Wallet balance under concurrent changes (optimistic concurrency retry), idempotent references | `Billing/WalletServiceTests.cs` (pre-existing) |
| Customer / partner portals: own data only, read-only, exports, card numbers never leave the API | `CustomerPortal/CustomerPortalApiTests.cs`, `PartnerPortal/*` (pre-existing) |
| Refresh-token rotation, reuse-detection, sliding expiry, suspension | `tests/VoltaX.AuthChecks` |
| Forged/stale role claims, DashboardAccessFilter, raw user CRUD closed, immutable card audit | `tests/VoltaX.AuthorizationChecks` |
| OCPI token auth (missing/non-OCPI → 401, token-A handshake-only), charger PKI (CSR CN pinning, chain, revocation) | `tests/VoltaX.OcpiChecks`, `tests/VoltaX.PkiChecks` |

## Findings

| # | Severity | Title | Status |
| --- | --- | --- | --- |
| 1 | High | Leaked secrets in git history (JWT key, AWS SNS key, Google OAuth client secret, SMTP password, production DB passwords) and in a pushed Docker image | **Owner action** — rotate + purge history (see below and `docs/security-runbook.md`) |
| 2 | Medium | Two entity-body endpoints bound whole models, inserting nested navigation rows and honouring client ids | **Fixed** in `1b14d24` |
| 3 | High | `AutoMapper` 12.0.1 has a known high-severity advisory (GHSA-rvv3-g6hj-g44x) | **Open** — AutoMapper removal is owned by the platform/performance agent; bump or removal pending there |
| 4 | Medium | Google Maps / Geocoding browser API keys committed in `VoltaXDashboardSpa/src/environments/*.ts` | **Owner action** — rotate and restrict to HTTP referrers + the specific APIs |
| 5 | Medium | Angular 16 and its ecosystem carry known XSS advisories (`npm audit`: 4 high, 8 moderate) | **Owner action / out of scope** — all fixes require the Angular 16→latest major upgrade, explicitly out of scope |
| 6 | Medium | Test accounts (`admin@eveon.ma`, `partner@eveon.ma`, `customer@eveon.ma`) are seeded and their passwords rotated/mailed on every start, including in `appsettings.production.json` | **Owner action** — set `FirstSetup__TestUsers__Enabled=false` in production once real accounts exist |
| 7 | Low | `google-callback.component.ts` passes `returnUrl` to `router.navigateByUrl` without the in-app-path guard used by `session.component.ts` | **Accepted / low** — `navigateByUrl` is client-side routing, not an external-redirect sink; recommend applying `safeReturnUrl` for consistency when the SPA is next touched |
| 8 | Low | HTML email templates (`PopulateTemplate`) interpolate some admin-entered placeholders (suspension reason, notice text) without HTML-encoding | **Accepted / low** — values are operator-entered and rendered in mail clients that do not execute script; the system-report template already HTML-encodes |
| 9 | Info | Password storage is PBKDF2 (ASP.NET Core Identity v3) with legacy HMAC hashes re-hashed on next sign-in; charger Basic auth uses PBKDF2-SHA256 (210k iterations) | **Verified** — no action |

No Critical code defect was found that is fixable in code; the Critical/High exposure is the
leaked-secret surface, which is remediated operationally (rotation + history purge).

### Controls verified (no change needed)

- Authorization is enforced by `DashboardAccessFilter` at an authorization stage (401 before
  model binding for anonymous callers) and a permission stage after binding; unmapped
  endpoints are admin-only; the access snapshot is re-read from the database on every
  request so suspension/role changes take effect immediately (test #4, `AuthenticationTests`).
- Dynamic LINQ list queries (`SafeDynamicQuery`) resolve every field against the entity's
  real properties, reject credential-named fields, cap path depth and term count, and bind
  values as typed parameters — injection attempts return 400 (`MassAssignmentTests`).
- Payment wallet credits are idempotent on a ledger reference and refuse amount/provider/
  order mismatches; only a signature-verified notification moves money (`PaymentNotificationTests`,
  `ProviderSignatureTests`).
- File uploads are validated by magic-byte sniffing (JPEG/PNG/WebP only, no SVG/HTML),
  size-capped, written with `FileMode.CreateNew` under a path-traversal guard; email
  templates and GDPR exports are stored outside `wwwroot`.
- CSV exports neutralise formula-injection prefixes (`=`,`+`,`-`,`@`) and quote separators
  (`Helpers/CsvField.cs`); invoices are generated as PDF, and the only browser-facing HTML
  (the sandbox payment page) HTML-encodes all interpolated values and ships a strict CSP.
- Security headers, HSTS (prod), CORS restricted to configured origins, and the HttpOnly /
  Secure / SameSite=Strict refresh cookie (`HttpSecurityTests`).
- SignalR hubs: `AuthorizedHubFilter` authorises every hub method — a user may join only
  its own session/user groups (or with `ViewChargingSessions`), and a charger group only
  with `ViewChargePoints` or partner ownership; `NotificationHub` joins the caller's own
  group only.

## Owner actions

These cannot be done from the codebase. `docs/security-runbook.md` has the step-by-step
procedure; the essentials:

1. **Rotate every secret that was ever committed** (treat as public): `AppSettings:Token`
   (JWT signing key — forces re-login), the AWS SNS access/secret key, the Google OAuth
   client secret(s), the SMTP password, and the production MySQL / SQL Server passwords.
   They appear in historical commits (e.g. `appsettings.json` / `appsettings.production.json`
   before they were blanked) and in the Docker image pushed to Docker Hub.
2. **Purge the secrets from git history** (filter-repo + force push; all collaborators
   re-clone) and make the Docker Hub image private or delete old tags.
3. **Rotate and restrict the Google browser keys** (`googleMapsApiKey`,
   `googleGeocodingApiKey` in `VoltaXDashboardSpa/src/environments/*.ts`): HTTP-referrer
   restriction to the real dashboard origin and API restriction to Maps/Geocoding only.
4. **Disable test accounts in production**: `FirstSetup__TestUsers__Enabled=false` once real
   admin/partner/customer accounts exist; stop mailing rotated passwords.
5. **Resolve the AutoMapper advisory** once the platform/performance agent lands the
   AutoMapper removal (or bump to a fixed version if it is kept).
6. **Plan the Angular upgrade** (16 → current) to clear the SPA XSS advisories; out of scope
   for this workstream.
7. **HTTPS / domain / OCPP TLS**, **charger passwords and certificates (profiles 2/3)**, and
   **Google API key restrictions** per the runbook; book an **external penetration test** of
   the API, dashboard, OCPP endpoint and payment flow against staging before launch.

## How to reproduce

```
dotnet test tests/VoltaX.Tests/VoltaX.Tests.csproj -c Release           # 171 tests
dotnet list VoltaXApi/VoltaXApi.csproj package --vulnerable --include-transitive
cd VoltaXDashboardSpa && npm audit --omit=dev
gitleaks detect --source . --no-banner --redact --log-opts=--all        # history
```
