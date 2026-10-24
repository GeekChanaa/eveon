using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Data;

namespace VoltaX.Tests.Api;

/// <summary>Database:Provider=Sqlite, the throwaway database of local and CI end-to-end runs (tools/e2e).</summary>
public class SqliteProviderTests
{
    private sealed class SqliteApi(string environment, string databaseFile) : WebApplicationFactory<VoltaXApi.Configurations.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            var settings = new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = new SqliteConnectionStringBuilder { DataSource = databaseFile, Pooling = false }.ToString(),
                ["AppSettings:Token"] = ApiFactory.TokenKey,
                ["AllowedHosts"] = "localhost",
                ["FirstSetup:Enabled"] = "false",
                ["Authentication:Google:ClientSecret"] = "test-google-secret",
                ["Database:InitializeRefreshTokens"] = "false",
                ["Payments:Provider"] = "",
                ["Security:HttpsRedirection"] = "false",
            };
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
            builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        }
    }

    [Fact]
    public async Task The_API_starts_on_a_new_SQLite_file_and_creates_its_schema()
    {
        var file = Path.Combine(Path.GetTempPath(), $"voltax-sqlite-{Guid.NewGuid():N}.db");
        try
        {
            await using (var api = new SqliteApi("Testing", file))
            {
                var live = await api.CreateClient().GetAsync("/health/live");

                Assert.True(live.IsSuccessStatusCode, $"/health/live answered {(int)live.StatusCode}");
                await using var scope = api.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                Assert.True(db.Database.IsSqlite());
                Assert.Equal(0, await db.ChargePoints.CountAsync());
                Assert.True(await db.Permissions.AnyAsync(), "the start-up seeders ran on the new database");
            }
            Assert.True(File.Exists(file));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(file); } catch (IOException) { /* still held by a finishing background task */ }
        }
    }

    [Fact]
    public void Production_refuses_the_SQLite_provider()
    {
        var file = Path.Combine(Path.GetTempPath(), $"voltax-sqlite-{Guid.NewGuid():N}.db");
        using var api = new SqliteApi("Production", file);

        var error = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("Database:Provider=Sqlite", error.ToString());
        Assert.False(File.Exists(file));
    }
}
