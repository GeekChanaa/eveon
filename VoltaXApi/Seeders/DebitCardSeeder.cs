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
            .RuleFor(dc => dc.CardNumber, f => f.Finance.CreditCardNumber())
            .RuleFor(dc => dc.ExpirationDate, f => f.Date.Future())
            .RuleFor(dc => dc.CVV, f => f.Finance.CreditCardCvv());

        var debitCards = debitCardFaker.Generate(quantity);
        await dbContext.DebitCards.AddRangeAsync(debitCards);
        return debitCards;
    }
}
