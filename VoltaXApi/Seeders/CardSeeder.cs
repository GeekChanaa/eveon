using System;
using System.Collections.Generic;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class CardSeeder
    {
        public async static Task<List<Card>> Seed(int count, List<User> users, VoltaXApiDbContext dbContext)
        {
            IRepository<Card> repo = new Repository<Card>(dbContext);
            var cardFaker = new Faker<Card>()
                .RuleFor(c => c.CardNumber, f => f.Finance.CreditCardNumber())
                .RuleFor(c => c.CardType, f => f.PickRandom("Visa", "Mastercard", "American Express"))
                .RuleFor(c => c.ExpirationDate, f => f.Date.Future())
                .RuleFor(c => c.MaxCount, f => f.Random.Number(100))
                .RuleFor(c => c.Status, f => f.PickRandom<CardStatusEnum>())
                .RuleFor(c => c.Balance, f => f.Random.Decimal(0, 1000))
                .RuleFor(c => c.Note, f => f.Lorem.Sentence())
                .RuleFor(c => c.UserID, f => f.PickRandom(users).ID);

            var cards = cardFaker.Generate(count);
            await repo.AddRangeAsync(cards);
            return cards;
        }
    }
}
