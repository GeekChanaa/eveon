using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Benchmarks;

/// <summary>
/// Times the statistics, list and lookup endpoints of the whole API (in memory, SQLite) on a seeded fleet of
/// 50 000 charging sessions. Runs only when VOLTAX_BENCHMARK names an output file; the numbers are in
/// docs/performance.md. The harness only uses the HTTP API and the model, so the same file can be run against an
/// older revision to compare.
/// </summary>
public class PerformanceBenchmark
{
    private const int Sessions = 50_000;
    private const int Stations = 20;
    private const int PointsPerStation = 5;
    private const int Cards = 2_000;
    private const int Orders = 5_000;
    private const int Iterations = 5;

    [Fact]
    public async Task Time_the_hot_endpoints()
    {
        var output = Environment.GetEnvironmentVariable("VOLTAX_BENCHMARK");
        if (string.IsNullOrEmpty(output))
            return;

        using var api = new ApiFactory();
        var seedClock = Stopwatch.StartNew();
        User admin = null!;
        int partnerId = 0, stationId = 0, chargePointId = 0;
        var now = DateTime.UtcNow;
        await api.Seed(async db =>
        {
            admin = await Seeds.User(db, "admin@bench.ma", role: "Admin");
            var partner = new Partner { Name = "Bench", Description = "", Email = "p@bench.ma", Phone = "0600000000", PartnerIdentificationNumber = "P-B" };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();
            partnerId = partner.ID;
            var connectors = new List<Connector>();
            for (var s = 0; s < Stations; s++)
            {
                var station = new ChargingStation { Name = $"VX{s:0000}", Address = "1 Rue", City = "Casablanca", PartnerID = s % 2 == 0 ? partner.ID : null };
                db.ChargingStations.Add(station);
                await db.SaveChangesAsync();
                if (s == 0) stationId = station.ID;
                for (var p = 0; p < PointsPerStation; p++)
                {
                    var point = new ChargePoint { ChargePointId = $"CP-{s}-{p}", ChargingStationID = station.ID };
                    db.ChargePoints.Add(point);
                    await db.SaveChangesAsync();
                    if (s == 0 && p == 0) chargePointId = point.ID;
                    for (var c = 1; c <= 2; c++)
                        connectors.Add(new Connector { ChargePointID = point.ID, EvseID = c, ConnectorID = 1, PricePerMinute = 0.5m });
                }
            }
            db.Connectors.AddRange(connectors);
            var users = Enumerable.Range(0, 200).Select(i => new User { Email = $"u{i}@bench.ma", FirstName = "U", LastName = i.ToString(), RoleID = admin.RoleID }).ToList();
            db.Users.AddRange(users);
            await db.SaveChangesAsync();
            var cards = Enumerable.Range(0, Cards).Select(i => Seeds.Card($"2026-{i:00000000}", 100, users[i % users.Count].ID)).ToList();
            db.Cards.AddRange(cards);
            await db.SaveChangesAsync();

            var random = new Random(1);
            var sessions = Enumerable.Range(0, Sessions).Select(i =>
            {
                var start = now.AddMinutes(-random.Next(1, 60 * 24 * 400));
                var billed = i % 10 != 0;
                return new ChargingSession
                {
                    ConnectorID = connectors[i % connectors.Count].ID,
                    UserID = users[i % users.Count].ID,
                    CardID = cards[i % cards.Count].ID,
                    StartDate = start,
                    EndDate = start.AddMinutes(30 + i % 90),
                    ChargedMinutes = 30 + i % 90,
                    ChargedKwhs = 5 + i % 40,
                    PricePerMinute = 0.5m,
                    PricePerIdleMinute = 0.2m,
                    IdleMinutes = i % 7,
                    EnergyAmount = billed ? 10 + i % 30 : null,
                    TimeAmount = billed ? 2.5m : null,
                    FlatFeeAmount = billed ? 1 : null,
                    TotalAmount = billed ? 13.5m + i % 30 : null
                };
            }).ToList();
            db.ChargingSessions.AddRange(sessions);
            await db.SaveChangesAsync();
            db.Transactions.AddRange(sessions.Select((s, i) => new Transaction
            {
                Uid = "tx-" + i, ChargingSessionID = s.ID, ConnectorID = s.ConnectorID, StartCardID = s.CardID, StartTime = s.StartDate,
                StopTime = s.EndDate, MeterStart = 0, MeterStop = (s.ChargedKwhs ?? 0) * 1000, Status = TransactionStatusEnum.Ended
            }));
            db.Orders.AddRange(Enumerable.Range(0, Orders).Select(i => new Order
            {
                CardID = cards[i % cards.Count].ID, Amount = 50, Status = RechargeOrderStatus.Completed, RechargeDate = now.AddMinutes(-random.Next(1, 60 * 24 * 400))
            }));
            await db.SaveChangesAsync();
        });
        seedClock.Stop();

        var client = api.Client(admin);
        var report = new StringBuilder();
        report.AppendLine($"Seeded {Sessions} sessions, {Sessions} transactions, {Orders} orders, {Cards} cards, {Stations * PointsPerStation * 2} connectors in {seedClock.Elapsed.TotalSeconds:0.0} s");
        report.AppendLine("| Endpoint | median ms | min ms |");
        report.AppendLine("| --- | ---: | ---: |");

        async Task Time(string name, Func<Task> call)
        {
            await call(); // warm up (query compilation)
            var times = new List<double>();
            for (var i = 0; i < Iterations; i++)
            {
                var watch = Stopwatch.StartNew();
                await call();
                times.Add(watch.Elapsed.TotalMilliseconds);
            }
            times.Sort();
            report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | {times[times.Count / 2]:0.0} | {times[0]:0.0} |"));
        }

        Task Get(string path) => client.GetAsync(path).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

        await Time("GET Statistics/TotalRevenue", () => Get("/api/Statistics/TotalRevenue"));
        await Time("GET Statistics/DailyRevenueLast30Days", () => Get("/api/Statistics/DailyRevenueLast30Days"));
        await Time("GET Statistics/MonthlyRevenueLastYear", () => Get("/api/Statistics/MonthlyRevenueLastYear"));
        await Time("GET partner/statistics/TotalRevenue", () => Get($"/api/partner/statistics/TotalRevenue/{partnerId}"));
        await Time("GET partner/statistics/MonthlyRevenueLastYear", () => Get($"/api/partner/statistics/MonthlyRevenueLastYear/{partnerId}"));
        await Time("GET partner/statistics/DailyChargingSessionsLast30Days", () => Get($"/api/partner/statistics/DailyChargingSessionsLast30Days/{partnerId}"));
        await Time("GET partner/statistics/MonthlyEnergyConsumedLastYear", () => Get($"/api/partner/statistics/MonthlyEnergyConsumedLastYear/{partnerId}"));
        await Time("GET Transaction/MonthlyEnergyConsumedLastYear", () => Get("/api/Transaction/MonthlyEnergyConsumedLastYear"));
        await Time("GET ChargingStation/GetChargingStationRevenue", () => Get($"/api/ChargingStation/GetChargingStationRevenue?chargingStationID={stationId}"));
        await Time("GET ChargingStation/GetChargingStationRevenueLast30Days", () => Get($"/api/ChargingStation/GetChargingStationRevenueLast30Days?chargingStationID={stationId}"));
        await Time("GET ChargingStation/GetChargingStationRevenueLast12Months", () => Get($"/api/ChargingStation/GetChargingStationRevenueLast12Months?chargingStationID={stationId}"));
        await Time("GET ChargingStation/GetTop10ChargingStationsByRevenue", () => Get("/api/ChargingStation/GetTop10ChargingStationsByRevenue"));
        await Time("GET partner/chargingStation/GetPartnerTop10ChargingStations", () => Get($"/api/partner/chargingStation/GetPartnerTop10ChargingStations/{partnerId}?pageSize=10"));
        await Time("GET Order/countByDay", () => Get("/api/Order/countByDay"));
        await Time("GET Order/countRechargeAmountByDay", () => Get("/api/Order/countRechargeAmountByDay"));
        await Time("GET Order/countByLast12Months", () => Get("/api/Order/countByLast12Months"));
        await Time("GET Card/GetAllCards?pageSize=-1", () => Get("/api/Card/GetAllCards?pageNumber=1&pageSize=-1"));
        await Time("GET ChargingSession/GetChargePointChargingSessions (page 1 of 25)", () => Get($"/api/ChargingSession/GetChargePointChargingSessions/{chargePointId}?pageNumber=1&pageSize=25"));
        await Time("GET access/me x20 (access check per request)", async () =>
        {
            for (var i = 0; i < 20; i++)
                await Get("/api/access/me");
        });
        await Time("Card lookup by number x100", () => api.Db(async db =>
        {
            for (var i = 0; i < 100; i++)
                await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.CardNumber == $"2026-{i * 17 % Cards:00000000}");
            return 0;
        }));
        await Time("Transaction lookup by Uid x100", () => api.Db(async db =>
        {
            for (var i = 0; i < 100; i++)
                await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Uid == $"tx-{i * 499 % Sessions}");
            return 0;
        }));

        await File.WriteAllTextAsync(output, report.ToString());
    }
}
