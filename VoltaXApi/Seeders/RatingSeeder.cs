using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using VoltaXApi.Models;
using VoltaXApi.Data;

namespace VoltaXApi.Data.Seeders
{
    public static class RatingSeeder
    {
        public static async Task<List<Rating>> Seed(int count, List<User> users, VoltaXApiDbContext dbContext)
        {
            IRepository<Rating> repo = new Repository<Rating>(dbContext);

            var ratingFaker = new Faker<Rating>()
                .RuleFor(o => o.Score, f => f.Random.Int(1, 5)) 
                .RuleFor(o => o.Comment, f => f.Lorem.Sentence()) 
                .RuleFor(o => o.UserID, f => f.PickRandom(users).ID) 
                .RuleFor(o => o.Entity, f => f.PickRandom("ChargePoint", "ChargingStation")) 
                .RuleFor(o => o.EntityID, f => f.Random.Int(1, 100)) 
                .RuleFor(o => o.IsDeleted, f => f.Random.Bool()) 
                .RuleFor(o => o.CreatedAt, f => f.Date.Past()) 
                .RuleFor(o => o.UpdatedAt, f => f.Date.Recent()); 

            var ratings = ratingFaker.Generate(count);

            await dbContext.Ratings.AddRangeAsync(ratings);
            await dbContext.SaveChangesAsync();

            return ratings;
        }
    }
}
