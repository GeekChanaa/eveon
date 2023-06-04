using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargePointSeeder
    {
        public static async Task<List<ChargePoint>> Seed(int count, List<ChargingStation> chargingStations, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargePoint> repo = new Repository<ChargePoint>(dbContext);

            var chargePointFaker = new Faker<ChargePoint>()
                .RuleFor(cp => cp.ChargingStationID, f => f.PickRandom(chargingStations).ID)
                .RuleFor(cp => cp.Network, f => f.Company.CompanyName())
                .RuleFor(cp => cp.Timezone, f => GenerateRandomTimeZone())
                .RuleFor(cp => cp.LastConnectTime, f => f.Date.Past())
                .RuleFor(cp => cp.OnlineTime, f => f.Date.Recent())
                .RuleFor(cp => cp.ChargingStation, f => f.PickRandom(chargingStations));

            var chargePoints = chargePointFaker.Generate(count);
            await repo.AddRangeAsync(chargePoints);
            return chargePoints;
        }

        private static string GenerateRandomTimeZone()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            var randomTimeZone = timeZones[new Random().Next(timeZones.Count)];
            return randomTimeZone.Id;
        }
    }
}
