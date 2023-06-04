using System;
using System.Collections.Generic;
using VoltaXApi.Models;
using Bogus;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargingStationSeeder
    {
        public static async Task<List<ChargingStation>> Seed(int count, List<CarCharger> carChargers, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargingStation> repo = new Repository<ChargingStation>(dbContext);
            var stationFaker = new Faker<ChargingStation>()
                .RuleFor(s => s.CarChargerID, f => f.PickRandom(carChargers).ID)
                .RuleFor(s => s.StationName, f => f.Company.CompanyName())
                .RuleFor(s => s.BusinessHours, f => f.Random.Words(2))
                .RuleFor(s => s.Address, f => f.Address.FullAddress())
                .RuleFor(s => s.CarCharger, f => f.PickRandom(carChargers));

            var stations = stationFaker.Generate(count);
            await repo.AddRangeAsync(stations);
            return stations;
        }
    }

}