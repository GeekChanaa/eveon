using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

namespace VoltaX.Tests.Api;

/// <summary>The list filter operators (FilterOperator) translated by EF against a real database.</summary>
public class FilterOperatorTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
        var owner = await Seeds.User(db, "owner@example.com");
        var a = Seeds.Card("ALPHA-1", 10, owner.ID, expires: Seeds.Now.AddDays(10));
        var b = Seeds.Card("BETA-2", 20, expires: Seeds.Now.AddDays(20));
        b.Status = CardStatusEnum.Blocked;
        var c = Seeds.Card("GAMMA-3", 30, expires: Seeds.Now.AddDays(30));
        c.Status = CardStatusEnum.Expired;
        c.Blocked = null;
        db.Cards.AddRange(a, b, c);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private VoltaXApiDbContext NewContext() => new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);

    private List<string> Numbers(GlobalParams p)
    {
        using var db = NewContext();
        return SafeDynamicQuery.Apply(db.Cards.AsQueryable(), p).Select(c => c.CardNumber).OrderBy(n => n).ToList();
    }

    private static GlobalParams Filter(params (string Field, string Op, string Value)[] terms) => new()
    {
        FilterBy = terms.Select(t => t.Field).ToArray(),
        FilterOperator = terms.Select(t => t.Op).ToArray(),
        FilterValue = terms.Select(t => t.Value).ToArray(),
        FilterMethod = "&&"
    };

    [Fact]
    public void In_matches_any_listed_enum_value()
        => Assert.Equal(new[] { "BETA-2", "GAMMA-3" }, Numbers(Filter(("Status", "in", "Blocked|Expired"))));

    [Fact]
    public void Contains_and_startswith_match_text()
    {
        Assert.Equal(new[] { "GAMMA-3" }, Numbers(Filter(("CardNumber", "contains", "MMA"))));
        Assert.Equal(new[] { "BETA-2" }, Numbers(Filter(("CardNumber", "startswith", "BE"))));
    }

    [Fact]
    public void Date_range_combines_two_operators()
    {
        var from = Seeds.Now.AddDays(15).ToString("o");
        var to = Seeds.Now.AddDays(25).ToString("o");
        Assert.Equal(new[] { "BETA-2" }, Numbers(Filter(("ExpirationDate", "gte", from), ("ExpirationDate", "lte", to))));
    }

    [Fact]
    public void Neq_excludes_the_value()
        => Assert.Equal(new[] { "ALPHA-1", "GAMMA-3" }, Numbers(Filter(("Status", "neq", "Blocked"))));

    [Fact]
    public void Missing_operator_means_equality()
        => Assert.Equal(new[] { "ALPHA-1" }, Numbers(new GlobalParams { FilterBy = new[] { "Status" }, FilterValue = new[] { "Active" } }));

    [Fact]
    public void Null_checks_whether_the_field_has_a_value()
    {
        Assert.Equal(new[] { "GAMMA-3" }, Numbers(Filter(("Blocked", "null", "true"))));
        Assert.Equal(new[] { "ALPHA-1", "BETA-2" }, Numbers(Filter(("Blocked", "null", "false"))));
    }

    [Fact]
    public void Operators_work_through_a_reference_navigation()
    {
        Assert.Equal(new[] { "ALPHA-1" }, Numbers(Filter(("User-Email", "in", "owner@example.com|other@example.com"))));
        Assert.Equal(new[] { "ALPHA-1" }, Numbers(Filter(("User-Email", "contains", "owner"))));
        Assert.Equal(new[] { "BETA-2", "GAMMA-3" }, Numbers(Filter(("UserID", "null", "true"))));
    }

    [Theory]
    [InlineData("Balance", "null", "true")]
    [InlineData("Blocked", "null", "maybe")]
    [InlineData("CardNumber", "gt", "A")]
    [InlineData("ExpirationDate", "contains", "2026")]
    [InlineData("Status", "like", "Active")]
    [InlineData("Status", "in", "Active|Unknown")]
    public void Invalid_operator_use_is_rejected(string field, string op, string value)
        => Assert.Throws<ValidationException>(() => Numbers(Filter((field, op, value))));

    [Fact]
    public void More_operators_than_fields_is_rejected()
        => Assert.Throws<ValidationException>(() => Numbers(new GlobalParams
        {
            FilterBy = new[] { "Status" }, FilterValue = new[] { "Active" }, FilterOperator = new[] { "eq", "eq" }
        }));
}
