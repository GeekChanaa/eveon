using Bogus;
using System.Collections.Generic;
using System.Linq;
using VoltaXApi.Models;
using VoltaXApi.Data;

public static class TransactionSeeder
{
    public static async Task<List<Transaction>> Seed(int number, IList<ChargeTag> chargeTags, IList<ChargePoint> chargePoints, VoltaXApiDbContext context)
    {
        // Transaction repo 
        IRepository<Transaction> _TransactionRepo = new Repository<Transaction>(context);
        var _faker = new Faker<Transaction>()
            .RuleFor(t => t.Uid, f => f.Random.Guid().ToString())
            .RuleFor(t => t.ChargePointID, f => f.PickRandom(chargePoints).ChargePointId)
            .RuleFor(t => t.ConnectorID, f => f.Random.Int(1, 100))
            .RuleFor(t => t.StartTagId, f => f.PickRandom(chargeTags).TagID)
            .RuleFor(t => t.StartTime, f => f.Date.Past(1)) // Starts in the last 6 months
            .RuleFor(t => t.MeterStart, f => f.Random.Double(1, 100))
            .RuleFor(t => t.StartResult, f => f.Random.Words(1))
            .RuleFor(t => t.StopTagId, f => f.PickRandom(chargeTags).TagID)
            .RuleFor(t => t.StopTime, (f, u) => u.StartTime.AddMinutes(f.Random.Double(10, 90))) // Stops between 10 minutes to 1 hour 30 minutes after start
            .RuleFor(t => t.MeterStop, f => f.Random.Double(1, 200)) // Power between 1 and 200
            .RuleFor(t => t.Amount, f => f.Random.Double(1, 200)) // Power between 1 and 200
            .RuleFor(t => t.StopReason, f => f.Random.Words(1));
            
        var Transactions = _faker.Generate(number).ToList();
        await _TransactionRepo.AddRangeAsync(Transactions);
        return Transactions;
    }
}
