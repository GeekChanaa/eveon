using Bogus;
using VoltaXApi.Models;


namespace VoltaXApi.Data.Seeders
{
    public static class OrderSeeder
    {
        public static async Task<List<Order>> Seed(int count, IList<Card> cards, VoltaXApiDbContext dbContext)
        {
            IRepository<Order> repo = new Repository<Order>(dbContext);
            var orders = new Faker<Order>()
                .RuleFor(o => o.CardID, f => f.PickRandom(cards).ID)
                .RuleFor(o => o.Amount, f => Math.Round(f.Random.Double(1, 100), 2))
                .RuleFor(o => o.RechargeDate, f => f.Date.Recent())
                .Generate(count);

            await dbContext.Orders.AddRangeAsync(orders);
            return orders;
        }
    }

}