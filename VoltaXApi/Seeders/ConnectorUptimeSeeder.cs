using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ConnectorUptimeSeeder
    {
        public static async Task<List<ConnectorUptime>> Seed(int count, IList<Connector> connectors, VoltaXApiDbContext dbContext)
        {
            IRepository<ConnectorUptime> repo = new Repository<ConnectorUptime>(dbContext);
            List<ConnectorUptime> chargePointUptimes = new List<ConnectorUptime>();
            for (int i = 0; i < count; i++)
            {
                var faker = new Faker<ConnectorUptime>()
                    .RuleFor(u => u.ConnectorID, f => f.PickRandom(connectors).ID)
                    .RuleFor(u => u.StartDate, f => f.Date.Past(1))
                    .RuleFor(u => u.EndDate, (f, u) => f.Date.Between(u.StartDate, DateTime.Now))
                    .RuleFor(u => u.ConnectorUptimeStatus, f => f.PickRandom<ConnectorUptimeStatusEnum>())
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