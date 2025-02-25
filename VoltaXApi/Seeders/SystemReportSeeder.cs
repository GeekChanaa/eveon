using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class SystemReportSeeder
    {
        public static async Task<List<SystemReport>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<SystemReport> repo = new Repository<SystemReport>(dbContext);
            var users = dbContext.Users.Select(u => u.ID).ToList();
            var cards = dbContext.Cards.Select(c => c.ID).ToList();
            var connectors = dbContext.Connectors.Select(c => c.ID).ToList();
            var chargePoints = dbContext.ChargePoints.Select(cp => cp.ID).ToList();
            
            var faker = new Faker<SystemReport>()
                .RuleFor(sr => sr.ReportCategory, f => f.PickRandom<ReportCategoryEnum>())
                .RuleFor(sr => sr.UserID, f => f.PickRandom(users))
                .RuleFor(sr => sr.CardID, f => f.PickRandom(cards))
                .RuleFor(sr => sr.ConnectorID, f => f.PickRandom(connectors))
                .RuleFor(sr => sr.ChargePointID, f => f.PickRandom(chargePoints))
                .RuleFor(sr => sr.ResolvedByID, f => f.PickRandom(users))
                .RuleFor(sr => sr.AssignedID, f => f.PickRandom(users))
                .RuleFor(sr => sr.IssueDescription, f => f.Lorem.Sentence())
                .RuleFor(sr => sr.IsEmail, f => f.Random.Bool())
                .RuleFor(sr => sr.IsNotification, f => f.Random.Bool())
                .RuleFor(sr => sr.Status, f => f.PickRandom<ReportStatusEnum>())
                .RuleFor(sr => sr.Criticality, f => f.PickRandom<ReportCriticality>())
                .RuleFor(sr => sr.IsDeleted, f => false)
                .RuleFor(sr => sr.CreatedAt, f => f.Date.Past(1))
                .RuleFor(sr => sr.UpdatedAt, f => f.Date.Recent());

            var systemReports = faker.Generate(count);
            await repo.AddRangeAsync(systemReports);
            await dbContext.SaveChangesAsync();
            return systemReports;
        }
    }
}
