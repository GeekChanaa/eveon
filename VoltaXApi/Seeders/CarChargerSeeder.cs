using System;
using System.Collections.Generic;
using VoltaXApi.Models;
using VoltaXApi.Data;
using Bogus;
namespace VoltaXApi.Data.Seeders
{
    public static class CarChargerSeeder
    {
        public async static Task<List<CarCharger>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<CarCharger> repo = new Repository<CarCharger>(dbContext);
            var chargerFaker = new Faker<CarCharger>()
                .RuleFor(c => c.Status, f => f.PickRandom("Available", "Unavailable"))
                .RuleFor(c => c.Location, f => f.Address.FullAddress());

            var chargers = chargerFaker.Generate(count);
            await repo.AddRangeAsync(chargers);
            return chargers;
        }
    }

}