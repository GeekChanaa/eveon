using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ConnectorSeeder
    {
        public static async Task<List<Connector>> Seed(int count, List<ChargePoint> chargePoints, VoltaXApiDbContext dbContext)
        {
            IRepository<Connector> repo = new Repository<Connector>(dbContext);
            var connectorFaker = new Faker<Connector>()
                .RuleFor(c => c.ChargePointID, f => f.PickRandom(chargePoints).ID)
                .RuleFor(c => c.ConnectorType, f => f.PickRandom("Type 1", "Type 2", "CHAdeMO", "CCS"))
                .RuleFor(c => c.Power, f => f.Random.Decimal(1, 50));

            var connectors = connectorFaker.Generate(count);
            await repo.AddRangeAsync(connectors);
            return connectors;
        }
    }
}
