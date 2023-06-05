using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class CustomerSeeder
    {
        public async static Task<List<Customer>> Seed(int count, List<User> users, VoltaXApiDbContext dbContext)
        {
            IRepository<Customer> repo = new Repository<Customer>(dbContext);
            var customerFaker = new Faker<Customer>()
                .RuleFor(c => c.UserID, f => f.PickRandom(users).ID)
                .RuleFor(c => c.Sold, f => f.Random.Bool())
                .RuleFor(c => c.User, f => f.PickRandom(users));

            var customers = customerFaker.Generate(count);
            await repo.AddRangeAsync(customers);
            return customers;
        }
    }
}
