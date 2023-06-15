using Bogus;
using VoltaXApi.Models;


namespace VoltaXApi.Data.Seeders
{
    public static class OrderSeeder
    {
        public static async Task<List<Order>> GetOrders(int count, IList<User> users, IList<Card> cards, VoltaXApiDbContext dbContext)
        {
            IRepository<Order> repo = new Repository<Order>(dbContext);
            var orders = new Faker<Order>()
                .RuleFor(o => o.ID, f => f.UniqueIndex + 1)  // assuming ID is auto-increment
                .RuleFor(o => o.UserID, f => f.PickRandom(users).ID)
                .RuleFor(o => o.CardID, f => f.PickRandom(cards).ID)
                .RuleFor(o => o.Amount, f => Math.Round(f.Random.Decimal(1m, 100m), 2))
                .RuleFor(o => o.RechargeDate, f => f.Date.Recent())
                .Generate(count);

            await dbContext.Orders.AddRangeAsync(orders);
            return orders;
        }
    }

}