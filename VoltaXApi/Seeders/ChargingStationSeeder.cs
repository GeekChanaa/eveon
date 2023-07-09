using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargingStationSeeder
    {
        public static async Task<List<ChargingStation>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargingStation> repo = new Repository<ChargingStation>(dbContext);
            var faker = new Faker<ChargingStation>()
                .RuleFor(cs => cs.Name, f => f.Company.CompanyName())
                .RuleFor(cs => cs.Address, f => f.Address.FullAddress())
                .RuleFor(cs => cs.Network, f => f.Company.CompanySuffix())
                .RuleFor(cs => cs.Category, f => f.PickRandom(new string[] { "public", "private", "partner" }))
                .RuleFor(cs => cs.ChargerQuantity, f => f.Random.Int(1, 10).ToString())
                .RuleFor(cs => cs.Country, f => f.Address.Country())
                .RuleFor(cs => cs.State, f => f.Address.State())
                .RuleFor(cs => cs.City, f => f.PickRandom(new string[] { "Tangier", "Agadir", "Rabat" }))
                .RuleFor(cs => cs.Latitude, f => f.Address.Latitude().ToString())
                .RuleFor(cs => cs.Longitude, f => f.Address.Longitude().ToString())
                .RuleFor(cs => cs.Organisation, f => f.Company.CompanyName())
                .RuleFor(cs => cs.ParkingType, f => f.Random.Bool() ? "Covered" : "Uncovered")
                .RuleFor(cs => cs.Status, f => f.Random.Bool() ? "Active" : "Inactive")
                .RuleFor(cs => cs.WifiAmenity, f => f.Random.Bool())
                .RuleFor(cs => cs.ParkingAmenity, f => f.Random.Bool())
                .RuleFor(cs => cs.RestaurantsAmenity, f => f.Random.Bool())
                .RuleFor(cs => cs.WashroomAmenity, f => f.Random.Bool())
                .RuleFor(cs => cs.SittingAreaAmenity, f => f.Random.Bool());

            var chargingStations = faker.Generate(count);
            await repo.AddRangeAsync(chargingStations);
            await dbContext.SaveChangesAsync();
            return chargingStations;
        }
    }
}