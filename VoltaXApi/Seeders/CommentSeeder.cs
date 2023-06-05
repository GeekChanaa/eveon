using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class CommentSeeder
    {
        public async static Task<List<Comment>> Seed(int count, List<User> users, List<ChargingStation> chargingStations, List<ChargePoint> chargePoints, VoltaXApiDbContext dbContext)
        {
            IRepository<Comment> repo = new Repository<Comment>(dbContext);
            var commentFaker = new Faker<Comment>()
                .RuleFor(c => c.UserID, f => f.PickRandom(users).ID)
                .RuleFor(c => c.Rating, f => f.Random.Int(1, 5))
                .RuleFor(c => c.Text, f => f.Lorem.Paragraph())
                .RuleFor(c => c.ChargingStationID, f => f.PickRandom(chargingStations).ID)
                .RuleFor(c => c.PointID, f => f.PickRandom(chargePoints).ID)
                .RuleFor(c => c.CommentTime, f => f.Date.Past())
                .RuleFor(c => c.User, f => f.PickRandom(users))
                .RuleFor(c => c.ChargingStation, f => f.PickRandom(chargingStations));

            var comments = commentFaker.Generate(count);
            await repo.AddRangeAsync(comments);
            return comments;
        }
    }
}
