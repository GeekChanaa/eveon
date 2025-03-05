using System;
using System.Collections.Generic;
using AutoMapper;
using Bogus;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class GlobalSeeder
    {
        public static async Task Seed(VoltaXApiDbContext context, IMapper mapper)
        {
             await ChargePointBrandsSeeder.Seed(context,mapper);
            //  await SqlScriptExecuter.ExecuteSqlScript(context);
             var partners = await PartnerSeeder.Seed(200,context);
             await BrandsAutomobilesSeeder.Populate(context);
            var chargeStations = await ChargingStationSeeder.Seed(100,context);
            var chargePoints = await ChargePointSeeder.Seed(100,chargeStations,context);
            var connectors = await ConnectorSeeder.Seed(100,chargePoints,context);
            var chargePointUptimes = await ChargePointUptimeSeeder.Seed(100,chargePoints,context);
            var connectorUptimes = await ConnectorUptimeSeeder.Seed(100,connectors,context);
            
            var users = await UserSeeder.Seed(100,context);
            var debitCards = await DebitCardSeeder.Seed(200,users,context);
            var cards = await CardSeeder.Seed(200, users,context);
            var orders = await OrderSeeder.Seed(100, cards, context);
            var chargeTags = await ChargeTagSeeder.Seed(100, cards, context);
            var ratings = await RatingSeeder.Seed(1000, users,context);

            var chargingSessions = await ChargingSessionsSeeder.Seed(1000,connectors,context);
            var transactions = await TransactionSeeder.Seed(1000,connectors, chargeTags, chargePoints, chargingSessions, context);
            await SystemReportSeeder.Seed(200,context);
        }

        private static string GenerateRandomTimeZone()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            var randomTimeZone = timeZones[new Random().Next(timeZones.Count)];
            return randomTimeZone.Id;
        }
    }
}
