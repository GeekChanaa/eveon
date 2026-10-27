# Database and migrations

## Provider story

| Where | Provider | Setting |
| --- | --- | --- |
| Production (EC2 API + Hostinger) | MariaDB through Pomelo | `Database__Provider=MySql`, `Database__ServerVersion=<SELECT VERSION()>` |
| EC2 / self-hosted | either | same settings |
| Local development | SQL Server (the default when `Database:Provider` is unset) or MariaDB (`docker-compose.dev.yml`) | user-secrets or environment |
| Tests (`tests/VoltaX.Tests`) | SQLite in memory, schema from the model (`EnsureCreated`) | none |

**One migration set serves both providers.** The schema only changes through the EF Core migrations in
`VoltaXApi/Migrations`; nothing creates or patches tables at start up any more (the raw SQL initializers
`Data/RefreshTokenSchema.cs` and `Data/PhoneLoginSchema.cs` were replaced by migrations).

- `20251214163619_recreate` (the baseline) was scaffolded against MySQL. On MySQL / MariaDB it runs as
  generated. On other providers `Migrations/MySqlScaffoldedOperations.cs` rewrites its operations so SQL
  Server gets the schema its model describes (provider types, identity columns, NULL filters on unique
  indexes, full-length names).
- Every later migration is **hand written and provider neutral**: no `type:` on columns, both identity
  annotations (`SqlServer:Identity` and `MySql:ValueGenerationStrategy`), provider-specific SQL behind
  `migrationBuilder.ActiveProvider`, and guarded (`IF OBJECT_ID(...) IS NULL`, `CREATE TABLE IF NOT EXISTS`,
  `ADD COLUMN IF NOT EXISTS`) when some databases may already have the object.
- `VoltaXApiDbContextModelSnapshot.cs` is **SQL Server typed**. `dotnet ef migrations has-pending-model-changes`
  is therefore meaningful only with the SQL Server provider (on MySQL it always reports differences, which is
  why the MySQL registration logs that check at Debug level).

### Adding a migration

1. Change the model.
2. Scaffold with the SQL Server provider so the snapshot diff stays small:
   `dotnet ef migrations add <Name> --project VoltaXApi` (with `Database__Provider` unset or `SqlServer`).
3. Edit the generated `Up`/`Down` into provider neutral code (see above), delete the generated
   `.Designer.cs` and put `[DbContext(typeof(VoltaXApiDbContext))]` and `[Migration("<id>_<Name>")]` on the
   class, as the existing hand written migrations do. Keep the snapshot change.
4. Apply it to both providers from empty and from the previous migration (`docker-compose.dev.yml` starts a
   MariaDB; any SQL Server 2022 works), e.g. `dotnet run --project VoltaXApi -- --migrate`.
5. `dotnet ef migrations has-pending-model-changes --project VoltaXApi` (SQL Server provider) must report
   no changes.

## Applying migrations

The API applies nothing by itself unless asked:

| How | When to use |
| --- | --- |
| `dotnet VoltaXApi.dll --migrate` (container: `docker run --rm --env-file api.env <image> --migrate`) | The deploy step. Applies the pending migrations, checks that every mapped table and column exists, exits (code 0, or non-zero with the error). `VoltaXApi/deploy.sh` runs it before replacing the running container. |
| `Database__MigrateOnStartup=true` | Hosts without a pre-deploy hook and a single instance. Same as `--migrate`, then the API starts. |
| `dotnet ef database update --project VoltaXApi` | From a developer machine allowed to reach the database. |
| `dotnet ef migrations script --idempotent -o schema.sql --project VoltaXApi` | When the database is only reachable through a console (phpMyAdmin): import the script. Generate it with `Database__Provider` set to the target provider. |

Without either option, a start with pending migrations **fails in Production** with the list of missing
migrations (and only logs a warning in other environments). `Database__FailOnPendingMigrations=false` turns
the failure into a warning; `true` makes other environments fail too.

Before migrating production, take a backup (`mysqldump --single-transaction --routines <db> > backup.sql`,
or Hostinger's hPanel backup). MySQL / MariaDB do not run DDL in transactions: a migration that fails half
way leaves the statements before the failure applied, and the guarded migrations can be re-run.

### Replaced start-up options

| Before | Now |
| --- | --- |
| `--initialize-refresh-tokens` (SQL Server only) | `--migrate` (`20260912100000_AddRefreshTokens`) |
| `Database__InitializePhoneLogin=true` | `--migrate` (`20261027000000_AddPhoneLoginChallenges`, idempotent: keeps a table the initializer created) |
| `Database__InitializeRefreshTokens` | removed |
| Start-up check of `RefreshTokens` only | start-up check of every migration (above) |

### A database that the migrations did not create

A database built some other way (an old SQL Server development database, a manual import) has no or an
incomplete `__EFMigrationsHistory`, and `--migrate` would try to create tables that exist. Either rebuild it
from empty with `--migrate`, or baseline it: insert into `__EFMigrationsHistory` (`MigrationId`,
`ProductVersion` = `9.0.0`) one row per migration whose changes the database already has, then run
`--migrate` for the rest. `--migrate` ends with a schema check that names any missing table or column.

## Verification (2026-10-23)

Throwaway containers, every migration applied to an empty database through the API's `--migrate`, then
the result compared table by table, column by column, index by index and foreign key by foreign key with the
schema `EnsureCreated` builds from the model on the same provider:

| Provider | Result |
| --- | --- |
| MariaDB 10.11.19 (`mariadb:10.11`) | 19 migrations applied (about 6 s), schema check passed. Output of `recreate` byte-identical to before (MySQL path unchanged); the only schema change is `PhoneLoginChallenges`. |
| MariaDB 11.8 (`mariadb:11.8`, the production major version) | 19 migrations applied, schema check passed, same differences as 10.11. |
| SQL Server 2022 (16.0, `mcr.microsoft.com/mssql/server:2022-latest`) | 19 migrations applied (about 4 s), schema check passed. Before this change `recreate` failed at its first table (`Cannot find data type longtext`). |
| Both | A database where the old initializer had already created `PhoneLoginChallenges` migrates without error. |

The CI job `migrations` (`.github/workflows/ci.yml`) repeats the `--migrate` run on every push against
empty MariaDB 11.8 and SQL Server 2022 databases, and `api` checks that the model has no change without
a migration (`dotnet ef migrations has-pending-model-changes`).

Known, harmless differences between the migrated schema and the model (left as they are):

- Columns the hand written migrations gave a length the model does not declare (model: unbounded):
  `ChargingProfiles.ChargingLimitSource` (16), `.LastError` (512), `.TransactionId` (36),
  `ChargingStrategies.Name` (100), `.Description` (500), `EvChargingNeeds.RequestedEnergyTransfer` (32),
  `StationLoadAllocations.Reason` (512), `.SendStatus` (32). Values longer than that fail to save; add
  `HasMaxLength` to the model (or widen the columns) if that can happen.
- `UserPhoneConflicts`: archive table written by `UniqueUserPhone`, not mapped by the model.
- Defaults (`DEFAULT 0`, `DEFAULT ''`) that the migrations set on columns added to existing tables.
- MariaDB: `CardChangeHistories.ChangeSetID` uses `utf8mb4` where the model would use `ascii`.
- SQL Server: unique indexes created by hand written migrations carry an `IS NOT NULL` filter even on
  non-nullable columns (EF adds it when a migration has no target model); the filter never excludes a row.
