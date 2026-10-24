# API performance

What the API does to stay fast as the fleet grows, the settings involved and the measured effect.

## Paging

Every list built with `PagedList` (the `GlobalParams` endpoints: `pageNumber`, `pageSize`, `OrderBy`, ...)

- reads the page with `CountAsync` + `Skip/Take` + `ToListAsync`, without change tracking, and does not query
  rows past the last page;
- never returns more than `Pagination:MaxPageSize` rows (default **100**). A missing, zero or negative page size
  asks for the largest page; a page number below 1 is the first page. The `Pagination` response header carries
  the page size actually used.

Before, `pageSize <= 0` returned the whole table. Four dashboard pickers still ask for `pageSize=-1` (cards in
"create recharge order", "start transaction", "create system report"; partners in "create user"): they now get
the first 100 rows. Raise `Pagination__MaxPageSize` if a fleet has more cards than that until those pickers search
on the server.

## Statistics in the database

| Area | Before | Now |
| --- | --- | --- |
| Operator / partner revenue, sessions, minutes, energy | every ended session of the period (all of them for totals) loaded and added up in memory | one aggregate query; daily / monthly series are one `GROUP BY` on the business period |
| Energy series (`Transaction/*EnergyConsumed*`) | start time + energy of every completed transaction of the period loaded | one `GROUP BY` on the business period |
| Station revenue (total, 7 / 30 days, 12 months) | one query per charge point (whole tracked sessions), repeated per day or month: up to 30 x charge points queries | one query per request |
| Top 10 stations by revenue (operator, partner) | the station revenue above for every station | one query grouped by station |
| Recharge order counters (`Order/count*`) | one query per day (30) or month (12) | one query per series (`OrderStatisticsService`) |
| Card list (`Card/GetAllCards`) | whole cards with their owner's user row, mapped afterwards | projected to the DTO in the query |

Business days and months are calendar periods of the business time zone (`Business:TimeZone`, see
`IBusinessClock`). A series groups rows with a single `CASE` over the periods' UTC boundaries
(`BusinessPeriods.IndexOf`); the boundaries are query parameters, so the query is the same for SQL Server, MariaDB
and SQLite and exact for any time zone. Rows that start after the last boundary (a charger clock ahead) are read
one by one and keyed by their own start, as before.

Revenue is `ChargingSession.TotalPriceWithVAT`: the database adds up the stored energy, time, flat fee and idle
amounts of billed sessions; only sessions billed before those amounts existed (`TotalAmount` null) are read and
priced row by row. `StatisticsAggregationTests` and `OrderStatisticsTests` compare every figure with the
row-by-row computation, in Casablanca and Kolkata (UTC+5:30) time. Figures are unchanged, except that decimal sums
no longer carry floating point noise (`1064.3` instead of `1064.3000000000002`) and daily dictionaries are in date
order.

## Indexes

Migration `20261026010000_QueryIndexes` (hand-written for SQL Server and MariaDB):

| Index | Used by |
| --- | --- |
| `Cards(CardNumber)` | OCPP Authorize / StartTransaction / TransactionEvent, OCPI tokens, reservations |
| `Transactions(Uid)` | TransactionEvent, StopTransaction, remote stop, operator search |
| `Transactions(StartTime)` | energy series, latest transactions |
| `ChargingSessions(UserID, EndDate)` | a driver's current session (polled by the app), history |
| `ChargingSessions(ConnectorID, StartDate)` | charge point / station / partner windows and lists |
| `ChargingSessions(StartDate)` | operator-wide windows |
| `Orders(RechargeDate)` | order counters and lists |
| `MessageLogs(ChargePointId, LogTime)` | a charger's message log |

`Cards.CardNumber` and `Transactions.Uid` become `nvarchar(64)` / `varchar(64)` and `MessageLogs.ChargePointId`
`(450)` (the length of `ChargePoints.ChargePointId`), so they can be index keys. The single-column
`ChargingSessions` indexes on `ConnectorID` and `UserID` are dropped once the composite ones exist (those still
serve the foreign keys).

Before applying it in production:

- check no value is longer than the new column (the `ALTER` fails rather than truncating on SQL Server; MariaDB
  without strict mode would truncate): `SELECT MAX(LEN(CardNumber)) FROM Cards`, `... LEN(Uid) FROM
  Transactions`, `... LEN(ChargePointId) FROM MessageLogs` (`CHAR_LENGTH` on MariaDB);
- `MessageLogs` and `Transactions` are the largest tables: the column change rewrites them. Run it in a quiet
  window, after the retention job has pruned old message logs.

## Access checks

`AccessService.Resolve` (every API request and SignalR hub call) loaded the user with role, role permissions,
permissions and image each time. Granted access is now kept in `AccessSnapshotCache` for
`Authorization:AccessCacheSeconds` (default **10**, `0` turns the cache off). It is dropped as soon as access may
change:

- `VoltaXApiDbContext` notifies the cache after a save that modifies or deletes a user (that user) or touches a
  role, permission, role permission or image (everyone);
- `HubConnections` revocations (role assignment, role permissions, suspension, deletion) drop the users' entries,
  also when another instance publishes them over the scale-out bus.

An entry loaded while an invalidation happened is never served, and refused users are not cached. With several
instances, a change made through another path than those above (a profile name, an image) can show on another
instance for at most the cache lifetime.

## Measurements

`tests/VoltaX.Tests/Benchmarks/PerformanceBenchmark.cs` seeds 50 000 sessions (+ 50 000 transactions, 5 000 orders,
2 000 cards, 200 connectors on 20 stations, over 400 days) and times the endpoints through the whole API (in
memory, SQLite). It only runs when `VOLTAX_BENCHMARK` names an output file:

```bash
VOLTAX_BENCHMARK=bench.md dotnet test tests/VoltaX.Tests/VoltaX.Tests.csproj -c Release --filter PerformanceBenchmark
```

Median of 5 runs after a warm-up, in ms, Windows 11 / .NET 9 / SQLite in memory. "Before" is the same harness on
the revision before this work (`5a93b09`), so without the indexes of the migration.

| Endpoint | Before | After |
| --- | ---: | ---: |
| GET Statistics/TotalRevenue | 329.7 | 172.2 |
| GET Statistics/DailyRevenueLast30Days | 48.3 | 33.2 |
| GET Statistics/MonthlyRevenueLastYear | 497.8 | 204.8 |
| GET partner/statistics/TotalRevenue | 313.7 | 62.0 |
| GET partner/statistics/MonthlyRevenueLastYear | 312.4 | 87.7 |
| GET partner/statistics/DailyChargingSessionsLast30Days | 49.3 | 9.2 |
| GET partner/statistics/MonthlyEnergyConsumedLastYear | 341.9 | 60.7 |
| GET Transaction/MonthlyEnergyConsumedLastYear | 186.6 | 70.6 |
| GET ChargingStation/GetChargingStationRevenue | 86.5 | 10.7 |
| GET ChargingStation/GetChargingStationRevenueLast30Days | 318.5 | 2.9 |
| GET ChargingStation/GetChargingStationRevenueLast12Months | 200.2 | 10.7 |
| GET ChargingStation/GetTop10ChargingStationsByRevenue | 1004.8 | 81.8 |
| GET partner/chargingStation/GetPartnerTop10ChargingStations | 426.7 | 58.1 |
| GET Order/countByDay | 21.8 | 2.2 |
| GET Order/countRechargeAmountByDay | 24.2 | 2.7 |
| GET Order/countByLast12Months | 11.4 | 5.7 |
| GET Card/GetAllCards?pageSize=-1 (2 000 cards; now capped at 100) | 35.8 | 2.8 |
| GET access/me x 20 (access check per request) | 23.7 | 8.9 |
| Transaction lookup by Uid x 100 | 269.5 | 12.4 |
| Card lookup by number x 100 (2 000 cards) | 18.0 | 13.1 |

SQLite stores decimals as text and sums them with a function call, so the revenue totals are the slowest figures
here; SQL Server and MariaDB sum decimals natively. The lookups by card number and transaction id gain most on a
real table: without the index each Authorize or TransactionEvent scans `Cards` or `Transactions`.
