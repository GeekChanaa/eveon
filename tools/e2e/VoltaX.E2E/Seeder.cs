using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaX.E2E;

/// <summary>A charge point to create: OCPP identity, Basic-auth password, provisioned, EVSE 1 / connector 1 at <see cref="Tariff"/>.</summary>
public sealed record SeedChargePoint(string Id, string Password);

/// <summary>A customer with one active card.</summary>
public sealed record SeedDriver(string Email, string Password, string? CardNumber, decimal Balance);

/// <summary>Creates the throwaway SQLite database from the API's model and fills it with the run's chargers and drivers.</summary>
public static class Seeder
{
    public static string ConnectionString(string databaseFile) => $"Data Source={databaseFile};Default Timeout=60";

    public static VoltaXApiDbContext Open(string databaseFile) =>
        new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(ConnectionString(databaseFile)).Options);

    public static async Task CreateAsync(string databaseFile, IEnumerable<SeedChargePoint> chargePoints, IEnumerable<SeedDriver> drivers,
        CancellationToken ct)
    {
        if (File.Exists(databaseFile))
            File.Delete(databaseFile);
        await using var db = Open(databaseFile);
        await db.Database.EnsureCreatedAsync(ct);
        // Readers do not wait for the writer (many chargers write at once).
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);

        var customer = new Role { Name = "Customer" };
        db.Roles.Add(customer);
        var station = new ChargingStation { Name = "E2E Station", Address = "1 Rue du Test", City = "Casablanca" };
        db.ChargingStations.Add(station);
        await db.SaveChangesAsync(ct);

        foreach (var cp in chargePoints)
        {
            var chargePoint = new ChargePoint
            {
                ChargePointId = cp.Id,
                SerialNumber = "SN-" + cp.Id,
                ChargingStationID = station.ID,
                Password = ChargePointPasswordHasher.Hash(cp.Password),
                SecurityProfile = 1
            };
            db.ChargePoints.Add(chargePoint);
            await db.SaveChangesAsync(ct);
            db.ChargePointProvisionings.Add(new ChargePointProvisioning { ChargePointID = chargePoint.ID, Status = ChargePointProvisioningStatusEnum.Provisioned });
            db.Connectors.Add(new Connector
            {
                ChargePointID = chargePoint.ID,
                EvseID = 1,
                ConnectorID = 1,
                PricePerKWh = Tariff.PricePerKWh,
                PricePerMinute = Tariff.PricePerMinute,
                FlatFee = Tariff.FlatFee,
                PricePerIdleMinute = Tariff.PricePerIdleMinute,
                MaxPower = 150
            });
        }
        await db.SaveChangesAsync(ct);

        foreach (var driver in drivers)
        {
            AuthHelper.CreatePasswordHash(driver.Password, out var hash, out var salt);
            var user = new User
            {
                Email = driver.Email,
                FirstName = "E2E",
                LastName = driver.Email.Split('@')[0],
                RoleID = customer.ID,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsEmailVerified = true,
                TermsAcceptedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            if (driver.CardNumber != null)
                db.Cards.Add(new Card
                {
                    CardNumber = driver.CardNumber,
                    Balance = driver.Balance,
                    Blocked = false,
                    ExpirationDate = DateTime.UtcNow.AddYears(5),
                    Status = CardStatusEnum.Active,
                    UserID = user.ID,
                    Note = "E2E"
                });
        }
        await db.SaveChangesAsync(ct);
    }
}
