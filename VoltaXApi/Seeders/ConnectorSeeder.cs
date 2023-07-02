using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ConnectorSeeder
    {
        public static async Task<List<Connector>> Seed(int count , IEnumerable<ChargePoint> chargePoints, VoltaXApiDbContext dbContext)
        {
            var chargePointIds = chargePoints.Select(cp => cp.ID).ToList();
            IRepository<Connector> repo = new Repository<Connector>(dbContext);
            List<Connector> connectors = new List<Connector>();
            for(int i = 1 ; i< 100 ; i++)
            {
                var connector = new Faker<Connector>()
                    .RuleFor(c => c.ChargePointID, i)
                    .RuleFor(c => c.ConnectorID, 1)
                    .RuleFor(c => c.ConnectorType, f => f.PickRandom(new string[] { "Type1", "Type2", "Type3" }))
                    .RuleFor(c => c.Power, f => f.Random.Decimal(1.0m, 1000.0m))
                    .RuleFor(c => c.Speed, f => f.Random.Double(1.0, 100.0)).Generate(1)[0];
                
                connectors.Add(connector);
            }
            await dbContext.Connectors.AddRangeAsync(connectors);
            await dbContext.SaveChangesAsync();
            return connectors;
        }
    }
}