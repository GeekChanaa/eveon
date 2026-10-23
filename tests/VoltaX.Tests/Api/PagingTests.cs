using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaX.Tests.Api;

public class PagingTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
        db.Cards.AddRange(Enumerable.Range(1, 150).Select(i => Seeds.Card($"CARD-{i:000}", i)));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private VoltaXApiDbContext NewContext() => new(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);

    [Theory]
    [InlineData(1, 500, 1, 100)]
    [InlineData(1, 0, 1, 100)]
    [InlineData(1, -1, 1, 100)]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-3, 5, 1, 5)]
    [InlineData(2, 100, 2, 100)]
    public async Task A_page_never_holds_more_than_the_maximum(int pageNumber, int pageSize, int expectedPage, int expectedSize)
    {
        await using var db = NewContext();

        var page = await PagedList<Card>.CreateAsync(db.Cards.OrderBy(c => c.ID), pageNumber, pageSize);

        Assert.Equal(Paging.DefaultMaxPageSize, Paging.MaxPageSize);
        Assert.Equal(expectedPage, page.CurrentPage);
        Assert.Equal(expectedSize, page.PageSize);
        Assert.Equal(150, page.TotalCount);
        Assert.Equal((int)Math.Ceiling(150 / (double)expectedSize), page.TotalPages);
        Assert.Equal(Math.Min(expectedSize, 150 - (expectedPage - 1) * expectedSize), page.Count);
        Assert.Equal((expectedPage - 1) * expectedSize + 1, page[0].ID);
    }

    [Fact]
    public async Task Pages_past_the_end_are_empty_without_reading_rows()
    {
        await using var db = NewContext();

        var page = await PagedList<Card>.CreateAsync(db.Cards.OrderBy(c => c.ID), 9, 50);

        Assert.Empty(page);
        Assert.Equal(150, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task Paged_entities_are_not_tracked()
    {
        await using var db = NewContext();

        var page = await PagedList<Card>.CreateAsync(db.Cards.OrderBy(c => c.ID), 1, 10);

        Assert.Equal(10, page.Count);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task In_memory_sources_are_paged_too()
    {
        var rows = Enumerable.Range(1, 30).ToList();

        var queried = await PagedList<int>.CreateAsync(rows.AsQueryable(), 2, 20);
        var listed = PagedList<int>.Create(rows, 3, 10);

        Assert.Equal(Enumerable.Range(21, 10), queried);
        Assert.Equal(2, queried.TotalPages);
        Assert.Equal(Enumerable.Range(21, 10), listed);
        Assert.Equal(30, listed.TotalCount);
    }

    [Fact]
    public async Task The_api_reports_the_capped_page_size()
    {
        using var api = new ApiFactory();
        User admin = null!;
        await api.Seed(async db =>
        {
            admin = await Seeds.User(db, "admin@test.ma", role: "Admin");
            db.Cards.AddRange(Enumerable.Range(1, 120).Select(i => Seeds.Card($"CARD-{i:000}", i)));
        });

        var response = await api.Client(admin).GetAsync("/api/Card/GetAllCards?pageNumber=1&pageSize=-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var header = JsonDocument.Parse(response.Headers.GetValues("Pagination").Single()).RootElement;
        Assert.Equal(100, header.GetProperty("itemsPerPage").GetInt32());
        Assert.Equal(120, header.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, header.GetProperty("totalPages").GetInt32());
        Assert.Equal(100, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }
}
