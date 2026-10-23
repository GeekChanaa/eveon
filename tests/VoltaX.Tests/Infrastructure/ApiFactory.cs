using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Services;

namespace VoltaX.Tests.Infrastructure;

/// <summary>
/// The whole API (middleware, global filters, controllers) in memory, on an in-memory SQLite database created
/// from the model. Background services are removed and mail is recorded. <see cref="Client"/> builds an
/// HttpClient signed in as a given user with a real access token.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<VoltaXApi.Configurations.Program>
{
    public const string TokenKey = "test-signing-key-test-signing-key-test-signing-key-test-signing-key-0123456789";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Dictionary<string, string?> _settings;

    public Recorder<IMailService> Mail { get; }
    private readonly IMailService _mailProxy;

    public ApiFactory(IDictionary<string, string?>? settings = null)
    {
        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Data Source=in-memory-tests",
            ["AppSettings:Token"] = TokenKey,
            ["AllowedHosts"] = "localhost",
            ["FirstSetup:Enabled"] = "false",
            ["Authentication:Google:ClientSecret"] = "test-google-secret",
            ["Database:InitializeRefreshTokens"] = "false",
            ["GlobalConfigurations:Vat"] = "0.2",
            ["GlobalConfigurations:GracePeriod"] = "300",
            ["GlobalConfigurations:DefaultMinimumRequiredBalance"] = "0",
            ["Payments:Provider"] = "Sandbox",
            ["Payments:Sandbox:Secret"] = "sandbox-test-secret-0123456789",
            ["Payments:PublicApiUrl"] = "http://localhost",
            ["Payments:SpaUrl"] = "http://spa.test",
        };
        if (settings != null)
            foreach (var (key, value) in settings)
                _settings[key] = value;

        var mail = Recorder<IMailService>.Create();
        Mail = mail.Recorder;
        _mailProxy = mail.Proxy;

        _connection.Open();
        using var db = new VoltaXApiDbContext(new DbContextOptionsBuilder<VoltaXApiDbContext>().UseSqlite(_connection).Options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in _settings)
            builder.UseSetting(key, value);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            // EF 9 keeps every AddDbContext configuration: drop the SQL Server one entirely.
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<VoltaXApiDbContext>>();
            services.RemoveAll<DbContextOptions<VoltaXApiDbContext>>();
            services.RemoveAll<VoltaXApiDbContext>();
            services.AddDbContext<VoltaXApiDbContext>((sp, options) => options.UseSqlite(_connection)
                .AddInterceptors(sp.GetRequiredService<VoltaXApi.Services.Audit.AuditSaveChangesInterceptor>()));
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IMailService>();
            services.AddSingleton(_mailProxy);
        });
    }

    public async Task<T> Db<T>(Func<VoltaXApiDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>());
    }

    public async Task Seed(Func<VoltaXApiDbContext, Task> seed)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }

    /// <summary>An HttpClient carrying an access token for <paramref name="user"/> (null: anonymous).</summary>
    public HttpClient Client(User? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (user != null)
        {
            using var scope = Services.CreateScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var token = jwt.GenerateToken(new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new(ClaimTypes.Email, user.Email ?? "")
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(descriptor);
    }
}
