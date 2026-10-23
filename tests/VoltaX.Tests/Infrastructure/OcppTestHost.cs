using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.Services;

namespace VoltaX.Tests.Infrastructure;

/// <summary>
/// The API's real OCPP handlers and services against a private database, without sockets:
/// <see cref="Call"/> feeds a frame to the request handler exactly like a connected charger would.
/// Mail, system reports, OCPI and load balancing are replaced by recorders; commands sent to chargers
/// go to <see cref="Sender"/>. The default database is EF in-memory (fast, not transactional); pass
/// <c>relational: true</c> for an in-memory SQLite database where SaveChanges is atomic and unique indexes
/// hold, as on SQL Server / MariaDB (money and concurrency tests).
/// </summary>
public sealed class OcppTestHost : IAsyncDisposable
{
    public const string Ocpp16 = OcppProtocols.Ocpp16;
    public const string Ocpp201 = OcppProtocols.Ocpp201;

    private readonly Dictionary<string, OcppConnection> _connections = new();
    private readonly SqliteConnection? _sqlite;
    private int _messageCounter;

    public ServiceProvider Provider { get; }
    public FakeCommandSender Sender { get; } = new();
    public Recorder<IMailService> Mail { get; }
    public Recorder<ISystemReportService> SystemReports { get; }

    public OcppTestHost(IDictionary<string, string?>? settings = null, Action<IServiceCollection>? configure = null, bool relational = false)
    {
        var values = new Dictionary<string, string?>
        {
            ["Ocpp:CommandTimeoutSeconds"] = "2",
            ["GlobalConfigurations:DefaultLowBalanceThresholdBuffer"] = "0",
            ["GlobalConfigurations:DefaultPricePerKwh"] = "2",
            ["GlobalConfigurations:Vat"] = "0.2",
            ["GlobalConfigurations:GracePeriod"] = "300"
        };
        if (settings != null)
            foreach (var (key, value) in settings)
                values[key] = value;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var mail = Recorder<IMailService>.Create();
        var reports = Recorder<ISystemReportService>.Create();
        Mail = mail.Recorder;
        SystemReports = reports.Recorder;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<IConfiguration>(config);
        services.AddSignalR();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        if (relational)
        {
            _sqlite = new SqliteConnection("DataSource=:memory:");
            _sqlite.Open();
            services.AddDbContext<VoltaXApiDbContext>(o => o.UseSqlite(_sqlite));
        }
        else
        {
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContext<VoltaXApiDbContext>(o => o.UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        }
        VoltaXApi.ServiceRegistration.ConfigureOptions(services, config);
        VoltaXApi.ServiceRegistration.ConfigureRepositories(services);
        VoltaXApi.ServiceRegistration.ConfigureFactories(services);
        VoltaXApi.ServiceRegistration.ConfigureApplicationServices(services);
        VoltaXApi.ServiceRegistration.ConfigureOCPPServices(services);
        VoltaXApi.ServiceRegistration.ConfigureOCPPHandlers(services);
        VoltaXApi.ServiceRegistration.ConfigureAutoMapper(services);
        // Outside world replaced: no mail, no OCPI, no real sockets for CSMS commands.
        services.AddSingleton(mail.Proxy);
        services.AddSingleton(reports.Proxy);
        services.AddSingleton(_ => Stub<IWebHostEnvironment>.Create());
        services.AddScoped<IExternalTokenAuthorizer, NoExternalTokens>();
        services.AddScoped<IRoamingTransactionObserver, NoRoaming>();
        services.AddSingleton<IOcppCommandSender>(Sender);
        services.AddSingleton<VoltaXApi.SmartCharging.ILoadBalancingTrigger, NoLoadBalancing>();
        configure?.Invoke(services);
        Provider = services.BuildServiceProvider();
        if (relational)
        {
            using var scope = Provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>().Database.EnsureCreated();
        }
    }

    public OcppConnection Connection(string chargePointId, string protocol = Ocpp201)
    {
        if (!_connections.TryGetValue(chargePointId, out var connection))
            _connections[chargePointId] = connection = new OcppConnection(chargePointId, protocol, new IdleWebSocket(protocol),
                new ChargePointStatus { Id = chargePointId, Protocol = protocol });
        return connection;
    }

    /// <summary>Sends a CALL and returns the raw reply frame (CALLRESULT or CALLERROR).</summary>
    public async Task<string> CallRaw(string chargePointId, string action, object payload, string protocol = Ocpp201)
    {
        var id = $"m{Interlocked.Increment(ref _messageCounter)}";
        var json = payload as string ?? JsonConvert.SerializeObject(payload, OCPPMessageFactory.DefaultSettings);
        var raw = OcppJson.SerializeCall(id, action, json);
        var frame = OcppJson.Parse(raw).Frame!;
        await using var scope = Provider.CreateAsyncScope();
        var requestHandler = scope.ServiceProvider.GetRequiredService<OCPPRequestHandler>();
        return await requestHandler.ProcessRequest(Connection(chargePointId, protocol), frame, raw);
    }

    /// <summary>Sends a CALL and returns the CALLRESULT payload; a CALLERROR fails the test.</summary>
    public async Task<JsonElement> Call(string chargePointId, string action, object payload, string protocol = Ocpp201)
    {
        var reply = await CallRaw(chargePointId, action, payload, protocol);
        var elements = JsonDocument.Parse(reply).RootElement.EnumerateArray().ToArray();
        if (elements[0].GetInt32() != 3)
            throw new Xunit.Sdk.XunitException($"{action} answered with CALLERROR {reply}");
        return elements[2].Clone();
    }

    public async Task<T> Query<T>(Func<VoltaXApiDbContext, Task<T>> query)
    {
        await using var scope = Provider.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>());
    }

    public async Task Seed(Func<VoltaXApiDbContext, Task> seed)
    {
        await using var scope = Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Runs <paramref name="action"/> with a service resolved from a fresh scope.</summary>
    public async Task<TResult> With<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        await using var scope = Provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task With<TService>(Func<TService, Task> action) where TService : notnull
    {
        await using var scope = Provider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        if (_sqlite != null)
            await _sqlite.DisposeAsync();
    }
}
