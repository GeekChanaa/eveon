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
                    .RuleFor(c => c.ChargePointID, f => f.PickRandom(chargePointIds))
                    .RuleFor(c => c.Power, f => f.Random.Decimal(1.0m, 1000.0m))  // Random power between 1.0 and 1000.0 kW
                    .RuleFor(c => c.ConnectorID, GenerateConnectorID())
                    .RuleFor(c => c.EvseID, GenerateConnectorID())
                    .RuleFor(c => c.ConnectorType, f => f.PickRandom(new string[] { "Type1", "Type2", "Type3" }))
                    .RuleFor(c => c.Power, f => f.Random.Decimal(1.0m, 1000.0m))  // Random power between 1.0 and 1000.0 kW
                    .RuleFor(c => c.Speed, f => f.Random.Double(1.0, 100.0))  // Random speed between 1.0 and 100.0
                    .RuleFor(c => c.PricePerKWh, f => f.Random.Decimal(0.1m, 1.0m))  // Random price per kWh between 0.1 and 1.0
                    .RuleFor(c => c.FlatFee, f => f.Random.Decimal(0m, 5.0m))  // Random flat fee between 0 and 5.0
                    .RuleFor(c => c.PricePerMinute, f => f.Random.Decimal(0m, 0.5m))  // Random price per minute between 0 and 0.5
                    .RuleFor(c => c.PricePerHour, f => f.Random.Decimal(1m, 10m))  // Random price per hour between 1 and 10
                    .RuleFor(c => c.MaxPower, f => f.Random.Decimal(1.0m, 1000.0m))  // Random max power between 1.0 and 1000.0 kW
                    .RuleFor(c => c.StartTime, f => f.Date.BetweenOffset(DateTimeOffset.Now.AddHours(-12), DateTimeOffset.Now).TimeOfDay)  // Random start time
                    .RuleFor(c => c.EndTime, f => f.Date.BetweenOffset(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(12)).TimeOfDay)  // Random end time
                    .Generate(1)[0];
                
                connectors.Add(connector);
            }
            await dbContext.Connectors.AddRangeAsync(connectors);
            await dbContext.SaveChangesAsync();
            return connectors;
        }

        public static int GenerateConnectorID()
        {
            Random random = new Random();
            return random.Next(100000, 10000000);
        }
    }
}