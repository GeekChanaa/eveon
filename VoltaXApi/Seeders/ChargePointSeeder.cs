using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;
using VoltaXApi.Data;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargePointSeeder
    {
        public async static Task<List<ChargePoint>> Seed(int quantity, VoltaXApiDbContext dbContext)
        {
            Randomizer randomizer = new Randomizer();

            IRepository<ChargePoint> repo = new Repository<ChargePoint>(dbContext);
            var chargePoints = new Faker<ChargePoint>()
                .RuleFor(o => o.ID, f => f.UniqueIndex)
                .RuleFor(o => o.ChargePointId, f => f.Random.AlphaNumeric(10))
                .RuleFor(o => o.Name, f => f.Vehicle.Manufacturer())
                .RuleFor(o => o.SerialNumber, f => f.Random.AlphaNumeric(15))
                .RuleFor(o => o.Make, f => f.Vehicle.Model())
                .RuleFor(o => o.Status, f => f.Random.Words(1))
                .RuleFor(o => o.Comment, f => f.Lorem.Sentence())
                .RuleFor(o => o.Username, f => f.Internet.UserName())
                .RuleFor(o => o.Password, f => f.Internet.Password())
                .RuleFor(o => o.ClientCertThumb, f => f.Random.AlphaNumeric(20))
                .RuleFor(o => o.Transactions, f => new List<Transaction>())
                .Generate(quantity);

            await repo.AddRangeAsync(chargePoints);
            return chargePoints;
        }
    }
}