using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class TransactionSeeder
    {
        public static async Task<List<Transaction>> Seed(int count, List<User> users, List<ChargePoint> chargePoints, VoltaXApiDbContext context)
        {
            IRepository<Transaction> repo = new Repository<Transaction>(context);
            
            var transactionFaker = new Faker<Transaction>()
                .RuleFor(t => t.UserID, f => f.PickRandom(users).ID)
                .RuleFor(t => t.ChargePointID, f => f.PickRandom(chargePoints).ID)
                .RuleFor(t => t.StartTime, f => f.Date.Past())
                .RuleFor(t => t.EndTime, f => f.Date.Recent())
                .RuleFor(t => t.EnergyConsumed, f => f.Random.Double(1, 100))
                .RuleFor(t => t.PaymentAmount, f => f.Random.Decimal(1, 100))
                .RuleFor(t => t.TransactionStatus, f => f.PickRandom(new[] { "Pending", "Completed", "Failed" }));

            var transactions = transactionFaker.Generate(count);
            try
            {
                await repo.AddRangeAsync(transactions);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("THISISISISIS");
                Console.WriteLine(ex.InnerException.ToString());
                throw;
            }
            return transactions;
        }
    }
}