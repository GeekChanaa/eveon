
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class CommentSeeder
    {
        public static async Task<List<Comment>> GenerateComments(int userCount, int stationCount, int chargePointCount, VoltaXApiDbContext dbContext)
        {
            IRepository<Comment> repo = new Repository<Comment>(dbContext);
            var faker = new Faker<Comment>()
                .RuleFor(c => c.UserID, f => f.Random.Int(1, userCount))
                .RuleFor(c => c.Rating, f => f.Random.Int(1, 5))
                .RuleFor(c => c.Text, f => f.Lorem.Sentence())
                .RuleFor(c => c.ChargingStationID, f => f.Random.Int(1, stationCount))
                .RuleFor(c => c.ChargePointID, f => f.Random.Int(1, chargePointCount))
                .RuleFor(c => c.CommentTime, f => f.Date.Past(3));

            var comments = faker.Generate(200);
            await repo.AddRangeAsync(comments);
            return comments;
        }
    }

}