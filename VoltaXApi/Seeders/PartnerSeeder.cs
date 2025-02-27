using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Data;

public static class PartnerSeeder
{
    public static async Task<List<Partner>> Seed(int number, VoltaXApiDbContext context)
    {
        // Partner repository
        IRepository<Partner> _partnerRepo = new Repository<Partner>(context);

        var fakePartners = new Faker<Partner>()
            .RuleFor(p => p.Name, f => f.Company.CompanyName())
            .RuleFor(p => p.Description, f => f.Company.CompanyName())
            .RuleFor(p => p.Type, f => f.PickRandom<PartnerTypeEnum>())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.Email2, f => f.Random.Bool() ? f.Internet.Email() : null)
            .RuleFor(p => p.Email3, f => f.Random.Bool() ? f.Internet.Email() : null)
            .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.Phone2, f => f.Random.Bool() ? f.Phone.PhoneNumber() : null)
            .RuleFor(p => p.Phone3, f => f.Random.Bool() ? f.Phone.PhoneNumber() : null)
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.Country, f => f.Address.Country())
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.TaxIdentificationNumber, f => f.Random.Bool() ? f.Finance.Account() : null)
            .RuleFor(p => p.RegistrationNumber, f => f.Random.Bool() ? f.Random.AlphaNumeric(10).ToUpper() : null)
            .RuleFor(p => p.BankAccountNumber, f => f.Random.Bool() ? f.Finance.Iban() : null)
            .RuleFor(p => p.IsDeleted, f => false) 
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(2))
            .RuleFor(p => p.UpdatedAt, (f, p) => f.Date.Between(p.CreatedAt, DateTime.UtcNow))
            .FinishWith((f, p) =>
            {
                Console.WriteLine($"Partner created. Id={p.ID}, Email={p.Email}, Type={p.Type}");
            });

        var partners = fakePartners.Generate(number).ToList();

        await _partnerRepo.AddRangeAsync(partners);
        return partners;
    }
}
