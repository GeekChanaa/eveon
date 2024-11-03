using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargePointUptimeSeeder
    {
        public static async Task<List<ChargePointUptime>> Seed(int count, IList<ChargePoint> chargePoints, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargePointUptime> repo = new Repository<ChargePointUptime>(dbContext);
            List<ChargePointUptime> chargePointUptimes = new List<ChargePointUptime>();
            for (int i = 0; i < count; i++)
            {
                var faker = new Faker<ChargePointUptime>()
                    .RuleFor(u => u.ChargePointID, f => f.PickRandom(chargePoints).ID)
                    .RuleFor(u => u.StartDate, f => f.Date.Past(1))
                    .RuleFor(u => u.EndDate, (f, u) => f.Date.Between(u.StartDate, DateTime.Now))
                    .RuleFor(u => u.ChargePointUptimeStatus, f => f.PickRandom<ChargePointUptimeStatusEnum>())
                    .RuleFor(u => u.IsDeleted, f => f.Random.Bool(0.1f))
                    .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
                    .RuleFor(u => u.UpdatedAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.Now));
  

                var chargePointUptime = faker.Generate();
                chargePointUptimes.Add(chargePointUptime);
            }
            await repo.AddRangeAsync(chargePointUptimes);
            await dbContext.SaveChangesAsync();
            return chargePointUptimes;
        }

        
    }
}