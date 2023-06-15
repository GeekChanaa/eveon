using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ConnectorTarifSeeder
    {
        public static async Task<List<ConnectorTarif>> Seed(int count, List<Connector> connectors, VoltaXApiDbContext dbContext)
        {
            IRepository<ConnectorTarif> repo = new Repository<ConnectorTarif>(dbContext);
            var connectorTarifFaker = new Faker<ConnectorTarif>()
                .RuleFor(o => o.ConnectorID, f => f.PickRandom(connectors).ID)
                .RuleFor(o => o.Unit, f => f.Random.Word())
                .RuleFor(o => o.Quantity, f => f.Random.Number(1, 100).ToString())
                .RuleFor(o => o.Currency, f => f.Finance.Currency().Code);

            var connectorTarifs = connectorTarifFaker.Generate(100);
            await dbContext.ConnectorTarifs.AddRangeAsync(connectorTarifs);
            await dbContext.SaveChangesAsync();
            return connectorTarifs;
        }
    }
}