using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;
using VoltaXApi.Data;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargePointSeeder
    {
        public static async Task<List<ChargePoint>> Seed(int count, List<ChargingStation> chargingStations, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargePoint> repo = new Repository<ChargePoint>(dbContext);
            var chargePointFaker = new Faker<ChargePoint>()
                .RuleFor(o => o.ChargePointId, f => f.Random.AlphaNumeric(10))
                .RuleFor(o => o.ChargingStationID, f => f.PickRandom(chargingStations).ID)
                .RuleFor(o => o.Name, f => f.Company.CompanyName())
                .RuleFor(o => o.SerialNumber, f => f.Random.AlphaNumeric(10))
                .RuleFor(o => o.Make, f => f.Vehicle.Manufacturer())
                .RuleFor(o => o.Status, f => f.PickRandom(new List<string> { "Active", "Inactive", "Maintenance" }))
                .RuleFor(o => o.Comment, f => f.Lorem.Sentence())
                .RuleFor(o => o.Username, f => f.Internet.UserName())
                .RuleFor(o => o.Password, f => f.Internet.Password())
                .RuleFor(o => o.ClientCertThumb, f => f.Random.AlphaNumeric(20));

            var chargePoints = chargePointFaker.Generate(100);
            await dbContext.ChargePoints.AddRangeAsync(chargePoints);
            await dbContext.SaveChangesAsync();
            return chargePoints;
        }

    }
}