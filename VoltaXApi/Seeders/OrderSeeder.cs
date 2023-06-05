using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class OrderSeeder
    {
        public async static Task<List<Order>> Seed(int count, List<User> users, List<Card> cards, VoltaXApiDbContext dbContext)
        {
            IRepository<Order> repo = new Repository<Order>(dbContext);
            var orderFaker = new Faker<Order>()
                .RuleFor(o => o.UserID, f => f.PickRandom(users).ID)
                .RuleFor(t => t.Amount, f => f.Random.Decimal(1, 100))
                .RuleFor(t => t.RechargeDate, f => f.Date.Recent())
                .RuleFor(o => o.CardID, f => f.PickRandom(cards).ID);

            var orders = orderFaker.Generate(count);
            await repo.AddRangeAsync(orders);
            return orders;
        }
    }
}
