# Refresh-token sessions

The API issues access/refresh pairs for customer registration, customer/partner password login, and Google login. Clients replace both tokens after each `POST /api/auth/Refresh`. Only SHA-256 hashes are stored in SQL Server. Rotation uses an optimistic concurrency check on `RevokedAt`, so concurrent requests cannot both spend the same token. Reusing a revoked token revokes the account's active refresh tokens.

## Repair an existing SQL Server database

From the repository root:

```powershell
dotnet run --project VoltaXApi -- --environment Development --initialize-refresh-tokens
```

This command creates only `RefreshTokens` and its indexes/foreign key, records `20260912100000_AddRefreshTokens` in EF migration history when it creates the table, verifies the mapped columns, and exits. It is transactional and serialized with a SQL application lock. Repeating it preserves existing tokens. It requires an existing VoltaX database with `Users`; it does not bootstrap the rest of the application.

Development startup also performs this targeted initialization before hosted services run. Set `Database:InitializeRefreshTokens` to `false` to require explicit deployment. Other environments verify the table and fail with an actionable message if it is missing. To deploy, run the same command with the intended environment and its SQL Server connection configured, or explicitly enable `Database:InitializeRefreshTokens` for initialization.

The checked-in production connection uses MySQL syntax, while service registration selects SQL Server. Configure `ConnectionStrings__DefaultConnection` with the actual SQL Server connection before deploying; do not use the development database for production.

Historical migrations and the snapshot were generated for MySQL. This targeted repair avoids replaying those migrations against SQL Server. A complete provider migration remains separate work. EF Core documents [separate migration sets for different providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers) and [EF 9 model mismatch checks](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes).

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
