using Bogus;
using System.Collections.Generic;
using System.Linq;
using VoltaXApi.Models;
using VoltaXApi.Data;

public static class TransactionSeeder
{
    public static async Task<List<Transaction>> Seed(int number, List<Connector> connectors, IList<ChargeTag> chargeTags, IList<ChargePoint> chargePoints, IList<ChargingSession> chargingSessions,VoltaXApiDbContext context)
    {
        // Transaction repo 
        IRepository<Transaction> _TransactionRepo = new Repository<Transaction>(context);
        var _faker = new Faker<Transaction>()
            .RuleFor(t => t.Uid, f => f.Random.Guid().ToString())
            .RuleFor(t => t.ConnectorID, f => f.PickRandom(connectors).ID)
            .RuleFor(t => t.ChargingSessionID, f => f.PickRandom(chargingSessions).ID)
            .RuleFor(t => t.StartCardID, f => f.PickRandom(chargeTags).ID)    
            .RuleFor(t => t.StartTime, f => f.Date.Past(1)) 
            .RuleFor(t => t.MeterStart, f => f.Random.Double(1, 100))
            .RuleFor(t => t.StartResult, f => f.Random.Words(1))
            .RuleFor(t => t.StopCardID, f => f.PickRandom(chargeTags).ID)
            .RuleFor(t => t.StopTime, (f, u) => u.StartTime.AddMinutes(f.Random.Double(10, 90))) 
            .RuleFor(t => t.MeterStop, f => f.Random.Double(1, 200)) 
            .RuleFor(t => t.Amount, f => f.Random.Double(1, 200)) 
            .RuleFor(t => t.StopReason, f => f.Random.Words(1));
            
        var Transactions = _faker.Generate(number).ToList();
        await _TransactionRepo.AddRangeAsync(Transactions);
        return Transactions;
    }
}
