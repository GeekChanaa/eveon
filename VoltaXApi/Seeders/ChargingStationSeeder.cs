using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargingStationSeeder
    {
        public static async Task<List<ChargingStation>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargingStation> repo = new Repository<ChargingStation>(dbContext);
            var moroccoCities = dbContext.Cities.Where(u => u.Country.Name == "Morocco").Select(u => u.Name).ToList();
            var faker = new Faker<ChargingStation>()
                .RuleFor(cs => cs.Name, f => f.Company.CompanyName())
                .RuleFor(cs => cs.Address, f => f.Address.FullAddress())
                .RuleFor(cs => cs.Network, f => f.PickRandom<ChargingStationNetworkEnum>())
                .RuleFor(cs => cs.Category, f => f.PickRandom<ChargingStationCategoryEnum>())
                .RuleFor(cs => cs.ChargerQuantity, f => f.Random.Int(1, 10).ToString())
                .RuleFor(cs => cs.Country, f => f.Address.Country())
                .RuleFor(cs => cs.State, f => f.Address.State())
                .RuleFor(cs => cs.City, f => f.PickRandom(moroccoCities))
                .RuleFor(cs => cs.Latitude, f => f.Address.Latitude().ToString())
                .RuleFor(cs => cs.Longitude, f => f.Address.Longitude().ToString())
                .RuleFor(cs => cs.Organisation, f => f.Company.CompanyName())
                .RuleFor(cs => cs.ParkingType, f => f.PickRandom<ParkingTypeEnum>())
                .RuleFor(cs => cs.Status, f => f.PickRandom<ChargingStationStatusEnum>())
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