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
            // var users = await UserSeeder.Seed(100,context);
            // var customers = await CustomerSeeder.Seed(100,users,context);
            // var cards = await CardSeeder.Seed(100,customers,context);
            // var chargingStations = await ChargingStationSeeder.Seed(100, context);
            // var chargePoints = await ChargePointSeeder.Seed(100,chargingStations, context);
            // var connectors = await ConnectorSeeder.Seed(100,chargePoints ,context);
            // var comments = await CommentSeeder.Seed(100,users,chargingStations,chargePoints,context);
            // var orders = await OrderSeeder.Seed(100,users,cards,context);
            // var transactions = await TransactionSeeder.Seed(100,users,chargePoints,context);
            var chargeStations = await ChargingStationSeeder.Seed(100,context);
            var chargePoints = await ChargePointSeeder.Seed(100,chargeStations,context);
            var connectors = await ConnectorSeeder.Seed(100,chargePoints,context);
            var connectorTarifs = await ConnectorTarifSeeder.Seed(100,connectors,context);
        }

        private static string GenerateRandomTimeZone()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            var randomTimeZone = timeZones[new Random().Next(timeZones.Count)];
            return randomTimeZone.Id;
        }
    }
}
