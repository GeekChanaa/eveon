# Refresh-token sessions

The API issues access/refresh pairs for customer registration, customer/partner password login, and Google login. Clients replace both tokens after each `POST /api/auth/Refresh`. Only SHA-256 hashes are stored in the database. Rotation uses an optimistic concurrency check on `RevokedAt`, so concurrent requests cannot both spend the same token. Reusing a revoked token revokes the account's active refresh tokens.

## Schema

`RefreshTokens` is created by the migration `20260912100000_AddRefreshTokens`, applied like every other
migration with `--migrate` on SQL Server and MySQL / MariaDB alike (see [database.md](database.md)). The
former SQL Server-only repair command `--initialize-refresh-tokens` and the `Database:InitializeRefreshTokens`
setting are gone; a start with that migration (or any other) pending fails in Production with the list of
missing migrations.

## Client behavior

- Customer and partner portals share the refresh endpoint and token storage.
- The interceptor refreshes near expiry, shares an in-flight refresh, and retries a failed authenticated request once. Web Locks serialize refreshes across tabs on supported secure origins (including localhost); otherwise sharing is per tab. It only sends bearer tokens to this API.
- Network errors and server errors preserve the session; rejected refresh credentials clear it. Guards refresh expired sessions before admitting navigation.
- Logout clears local credentials immediately and revokes the server token. A late refresh response cannot restore a logged-out or replaced session.
- Profile Security exposes sign out everywhere. Password reset/change also revokes refresh sessions. Already-issued access JWTs remain valid until expiry (60 minutes by default).
- Google callbacks deliver credentials in a fragment, and both portals remove callback credentials from browser history immediately.
- The Flutter folder is currently a UI prototype without an API authentication client. Its backend contract is the same login/refresh/logout JSON contract described here.

## Validation

```powershell
dotnet build VoltaXApi --no-restore
dotnet run --project tests/VoltaX.AuthChecks
cd VoltaXDashboardSpa
npm run build -- --configuration development
node node_modules/@angular/cli/bin/ng.js test --watch=false --browsers=ChromeHeadless --ts-config=tsconfig.auth-spec.json --include=src/_services/token-refresh.service.spec.ts --include=src/app/auth/token.interceptor.spec.ts
```

The focused TypeScript test config excludes unrelated legacy specs that currently do not compile.
The SQL Server checks create a test user inside a transaction and roll back all test changes. Run them from the repository root against the development database.
