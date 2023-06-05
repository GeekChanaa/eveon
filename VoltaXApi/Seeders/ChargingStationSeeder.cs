using System;
using System.Collections.Generic;
using VoltaXApi.Models;
using Bogus;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargingStationSeeder
    {
        public static async Task<List<ChargingStation>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargingStation> repo = new Repository<ChargingStation>(dbContext);
            var chargingStationFaker = new Faker<ChargingStation>()
                .RuleFor(c => c.Name, f => f.Company.CompanyName())
                .RuleFor(c => c.BusinessHours, f => f.Random.Words(2))
                .RuleFor(c => c.Address, f => f.Address.FullAddress());

            var stations = chargingStationFaker.Generate(count);
            await repo.AddRangeAsync(stations);
            return stations;
        }
    }

}