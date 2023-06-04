using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class GlobalSeeder
    {
        public static async Task Seed(VoltaXApiDbContext context)
        {
            var carChargers = await CarChargerSeeder.Seed(100,context);
            var chargingStations = await ChargingStationSeeder.Seed(100,carChargers, context);
            await ChargePointSeeder.Seed(100,chargingStations, context);
        }

        private static string GenerateRandomTimeZone()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            var randomTimeZone = timeZones[new Random().Next(timeZones.Count)];
            return randomTimeZone.Id;
        }
    }
}
