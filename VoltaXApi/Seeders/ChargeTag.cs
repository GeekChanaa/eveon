using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargeTagSeeder
    {
        public static async Task<List<ChargeTag>> Seed(int count, IList<Card> cards, VoltaXApiDbContext dbContext)
        {
            IRepository<ChargeTag> repo = new Repository<ChargeTag>(dbContext);
            List<ChargeTag> chargeTags = new List<ChargeTag>();
            for (int i = 0; i < count; i++)
            {
                var faker = new Faker<ChargeTag>()
                    .RuleFor(t => t.TagID, f => f.Random.AlphaNumeric(32))
                    .RuleFor(t => t.TagName, f => f.Name.FirstName())
                    .RuleFor(t => t.ParentTagId, f => f.Random.AlphaNumeric(32))
                    .RuleFor(t => t.ExpiryDate, f => f.Date.Future())
                    .RuleFor(t => t.Blocked, f => f.Random.Bool())
                    .RuleFor(t => t.CardID, cards[i].ID);  // Here we ensure each CardID is used exactly once

                var chargeTag = faker.Generate();
                chargeTags.Add(chargeTag);
            }
            await repo.AddRangeAsync(chargeTags);
            await dbContext.SaveChangesAsync();
            return chargeTags;
        }
    }
}