# Dashboard authorization

Administrators manage roles from **Account > Roles & permissions** (`/dashboard/roles`; `/dashboard/permissions` redirects there). Create a role or select an existing role, adjust its feature permissions, then save. Assign roles from **Users > open a user > Roles & permissions**. Updating a role changes access for everyone assigned to it. Admin always has every permission and cannot be reduced; Customer, Partner and custom-role permissions are editable. Customer grants also apply to new public registrations. Built-in role names remain reserved. Administrators cannot change their own role or remove the last active administrator.

## Enforcement

- Dashboard route guards and navigation use `/api/access/me`. Direct URLs are checked, including create/edit pages and nested realtime charging sessions. Unknown dashboard features are admin-only.
- The global MVC action filter authorizes API requests independently of the SPA. The database supplies the current user, role and grants on every request. JWT role/permission claims cannot grant access. Deleted and suspended accounts are denied.
- `EndpointPermissions` maps resource actions to separate view/create/edit/delete permissions. Charger commands require `OperateChargePoints`. Unmapped endpoints are admin-only; public authentication and map endpoints are explicitly listed. Add new endpoint mappings and authorization regression checks when extending the API.
- Dashboard grants use Global scope. Existing OwnOnly/PartnerOnly grants are never widened into global access. Customer and partner exceptions check the persisted resource owner.
- Raw user CRUD routes that could expose credentials or assign privileged fields are blocked. Own profile updates accept only first name, last name and birthday. Role assignment has a dedicated admin endpoint and serializable transaction. Generic entity creation ignores posted navigation graphs, and updates require matching route/body IDs.
- SignalR connections require an active account; group membership and commands check resource access. Role assignment, role permission changes and user edits/deletion disconnect affected connections on the current API process. In a multi-instance deployment, distribute those invalidation events across instances before relying on immediate revocation of existing subscriptions. HTTP requests and new hub invocations always recheck the database.

## Rollout and validation

Restart the API after deploying. Startup idempotently seeds missing permission catalog rows using the existing schema; no migration is required. Existing custom roles need explicit AccessDashboard and feature grants. Create/edit/delete grants require the corresponding View permission; role management validates these dependencies.

`dotnet run --project tests/VoltaX.AuthorizationChecks -p:UseAppHost=false --configuration Release` runs database-backed regression checks using the configured database. Test rows and changes are rolled back in one transaction. The SPA has focused access/route/avatar tests in `src/_services/access.service.spec.ts`.

The navbar uses the uploaded avatar or external account picture with an initials fallback. Profile settings use the same detail cards, tab navigation, visible edit controls and light/dark theme variables as equipment pages.
