using Bogus;
using VoltaXApi.Models;
using VoltaXApi.Data;
using System;
using System.Data.Entity;

public class DebitCardSeeder
{
    public static async Task<List<DebitCard>> Seed(int quantity, VoltaXApiDbContext dbContext)
    {

        IRepository<DebitCard> repo = new Repository<DebitCard>(dbContext);

        var users = await dbContext.Users.ToListAsync();

        Randomizer.Seed = new Random(8675309);

        var debitCardFaker = new Faker<DebitCard>()
            .RuleFor(dc => dc.UserID, f => f.PickRandom(users).ID)
            .RuleFor(dc => dc.Name, f => f.Name.FullName())
            .RuleFor(dc => dc.Brand, f => f.PickRandom<DebitCardTypeEnum>())
            .RuleFor(dc => dc.Last4, f => f.Random.ReplaceNumbers("####"))
            .RuleFor(dc => dc.ExpiryMonth, f => f.Random.Int(1, 12))
            .RuleFor(dc => dc.ExpiryYear, f => DateTime.UtcNow.Year + f.Random.Int(1, 5))
            .RuleFor(dc => dc.Provider, _ => "development-fake")
            .RuleFor(dc => dc.ProviderToken, f => "devtok_" + f.Random.AlphaNumeric(24));

        var debitCards = debitCardFaker.Generate(quantity);
        await dbContext.DebitCards.AddRangeAsync(debitCards);
        return debitCards;
    }
}
