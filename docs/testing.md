# Testing

| Layer | What | Command | Database |
| --- | --- | --- | --- |
| API tests | xUnit: endpoints and authorization, OCPP 1.6 / 2.0.1 scenarios on an in-process charger, billing, payments, portals | `dotnet test tests/VoltaX.Tests/VoltaX.Tests.csproj -c Release` | SQLite in memory |
| Console checks | `tests/VoltaX.*Checks`: hand-run checks of one area | `dotnet run --project tests/VoltaX.OcppChecks` (and so on) | development database |
| Simulator end-to-end | The real API process and the EVEON charger simulator (EVEON_SIM) over real WebSockets, every charging preset on both OCPP versions, then the sessions, amounts, VAT and wallet as the driver sees them | `tools/e2e/run-e2e.sh` / `tools/e2e/run-e2e.ps1` | SQLite file, thrown away |
| Load | Hundreds of simulated chargers charging at once against the same API process | `tools/e2e/run-e2e.sh load` | SQLite file, thrown away |
| Dashboard | Angular unit tests, headless Chrome | `cd VoltaXDashboardSpa && npm run test:ci` | none |
| Mobile app | Flutter tests | `cd volta_x_mobile && flutter test` | none |

## Throwaway SQLite database

`Database__Provider=Sqlite` runs the API on a SQLite file: there are no SQLite migrations, the schema is
created from the model at start up (`EnsureCreated`), and the API refuses to start with it in Production. It
exists for test runs only; `docs/database.md` covers the real providers. `ApiProcess.Environment` in
`tools/e2e/VoltaX.E2E/ApiProcess.cs` is a complete set of settings for such a run (environment `E2E`,
`ConnectionStrings__DefaultConnection=Data Source=<file>`, a random signing key, no mail, payments or OCPI).

## Simulator end-to-end run

`tools/e2e/VoltaX.E2E` drives the whole stack the way production sees it:

1. builds the simulator (`npm ci` when `node_modules` is missing, `npm run sim:build`);
2. creates a SQLite database from the API's model and seeds it: one station, one charge point per preset
   and OCPP version (`E2E-16-HAPPY`, `E2E-201-HAPPY`, ...) with a Basic-auth password (security profile 1),
   connector EVSE 1 / 1 at a fixed tariff (2.50 / kWh, 0.10 / min, 1.00 flat, 0.50 / idle minute, VAT 20 %,
   5 min grace), and per preset a customer with an active card holding the preset's card number;
3. starts the API built with the runner (a copy of `VoltaXApi/bin/Release`, so builds go on meanwhile) on a
   free local port, configured only through environment variables: no mail server, no payments, no OCPI;
4. runs every preset with the headless simulator (`sim-server --scenario ... --report ... --exit`), 8 at a
   time, each on its own charge point. For "Remote start and stop" the runner is the driver: it signs in,
   calls the mobile remote start once the car is plugged in, then stops the transaction of its running
   session (`api/my/sessions/active`);
5. checks, as each driver through the customer portal API (`api/my/sessions`, `api/my/wallet`,
   `api/my/cards`):
   - the simulator's own expectations passed (exit code 0);
   - one session per transaction the charger reported, closed, whose energy (meterStop - meterStart),
     duration and stop reason match what the charger sent (1.6 reasons mapped to the 2.0.1 vocabulary);
   - energy, time and flat fee amounts equal to the tariff, no idle fee after a session the charger ended,
     total = charge + idle, VAT split of the total;
   - one wallet debit per session for its charge, one idle fee movement when there is an idle fee;
   - card balance = opening balance - session totals, and the wallet ledger adds up to the card balance;
   - for the refused unknown card: no session at all and no accepted transaction.

Presets not in the matrix: the ones that need a CSMS command the runner does not send (UpdateFirmware), never
end their transaction (power cut forgotten: the stale session sweeper closes it hours later), or exercise the
network rather than billing (reconnect storm, zombie charger, boot pending). The list is
`tools/e2e/VoltaX.E2E/Scenarios.cs`.

Prerequisites: .NET 9 SDK, Node.js 20+, an EVEON_SIM checkout (`--sim <dir>`, `EVEON_SIM_DIR`, or an
`EVEON_SIM` folder next to this repository).

```bash
tools/e2e/run-e2e.sh                                # 15 presets x 2 versions, about 4 minutes
tools/e2e/run-e2e.sh --only "Balance" --version 1.6 # presets whose name contains "Balance", 1.6 only
tools/e2e/run-e2e.sh --no-sim-build --parallel 4    # simulator already built, fewer at a time
```

Everything lands in `tools/e2e/out/<run>/` (ignored by git): `summary.md` (one row per run, then the failed
checks and the simulator's failed assertions), `api.log`, one simulator log and transaction report per charge
point, and the database. The summary also lists the API's logged errors by message: not a check (EF Core logs
the invoice number collision the billing retries, for instance), but a new message there deserves a look.
Exit code: 0 passed, 1 a check failed, 2 the run could not be set up.

## Load run

```bash
tools/e2e/run-e2e.sh load                                  # 200 chargers (100 x 1.6, 100 x 2.0.1), 5 minutes
tools/e2e/run-e2e.sh load --chargers 500 --minutes 10 --interval 30 --stagger 20
```

The same database and API set-up with one charge point, customer and card per charger. One simulator
process per OCPP version runs its fleet (`sim-server --count`), connecting `--stagger` ms apart. Every
charger is plugged in and badged with its own card; then, for `--minutes`, the chargers send meter values
every `--interval` seconds while the runner samples every 5 s the fleet counters (messages, errors,
reconnects, round trip), the API process (CPU over all cores, working set) and the response time of a driver
polling their running session once a second. At the end every session is stopped.

Checks: every charger accepted, every card accepted and every connector charging, no reconnect and no
CALLERROR or timeout during the window, the session poll never failed, every message answered after the stop,
and in the database one closed, billed session per charger with one wallet debit each. `summary.md` holds the
figures and the checks, `load-samples.csv` the 5 s samples.

The figures describe the API on SQLite on the machine that ran it: they show regressions and the order of
magnitude, not production capacity (MariaDB / SQL Server on their own host, `docs/performance.md`).

## CI

The `e2e` job of `.github/workflows/ci.yml` runs the matrix once the repository variable
`EVEON_SIM_REPOSITORY` names the simulator repository (owner/name; `EVEON_SIM_REF` picks the branch,
`EVEON_SIM_TOKEN` is a secret with read access when that repository is private). Without the variable the job
is skipped. The run folder is uploaded as the `e2e-run` artifact.
