

using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class CustomerSeeder
    {
        public static async Task<List<Customer>> Seed(int count, IList<User> users, VoltaXApiDbContext dbContext)
        {
            IRepository<Connector> repo = new Repository<Connector>(dbContext);
            var customers = new Faker<Customer>()
                .RuleFor(c => c.UserID, f => f.PickRandom(users).ID)
                .RuleFor(c => c.Sold, f => f.Random.Bool())
                .Generate(count);
                
            await dbContext.Customers.AddRangeAsync(customers);
            return customers;
        }
    }
}