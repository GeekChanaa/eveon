using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Middlewares;
using VoltaXApi.Models;
using VoltaXApi.Services;
using VoltaXApi.Services.Setup;

namespace VoltaXApi.Configurations;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // builder.WebHost.ConfigureKestrel(options =>
        // {
        //     options.ListenAnyIP(5000);
        // });

        // Logging first, so a failure in any of the steps below is on record.
        LoggingConfiguration.ConfigureLogging(builder);

        try
        {
            Log.Information("Starting VoltaX API ({Environment})", builder.Environment.EnvironmentName);

            // Configure services
            ConfigureServices(builder);

            var app = builder.Build();

            var initializeRefreshTokens = args.Contains("--initialize-refresh-tokens");
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                await PermissionSeeder.Seed(db);
                try
                {
                    await OcppDefaultProfileSeeder.Seed(db);
                }
                catch (Exception ex)
                {
                    // Table missing until AddChargePointProvisioning is applied; the API still starts.
                    Log.Warning(ex, "Could not seed the default OCPP provisioning profile");
                }
                if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:InitializePhoneLogin"))
                    await PhoneLoginSchema.Initialize(db);
                if (initializeRefreshTokens || builder.Configuration.GetValue<bool?>("Database:InitializeRefreshTokens")
                    == true || (app.Environment.IsDevelopment() && builder.Configuration["Database:InitializeRefreshTokens"] == null))
                    await RefreshTokenSchema.Initialize(db);
                else
                    await RefreshTokenSchema.Verify(db);
                if (args.Contains(AdminBootstrap.Flag))
                {
                    await AdminBootstrap.Run(db, builder.Configuration);
                    return;
                }
            }
            if (initializeRefreshTokens)
                return;

            // Every start (so every deploy) checks the roles, defaults and test accounts.
            await RunFirstSetup(app);

            // Configure the HTTP request pipeline
            ConfigureApp(app);

            // Run database seeding
            //SeedDatabase(app);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            // A crash during start up never reaches a request logger, so it is caught here.
            Log.Fatal(ex, "VoltaX API terminated unexpectedly");
            throw;
        }
        finally
        {
            // Flushes whatever is still buffered in the file sink.
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Register all services
        ServiceRegistration.ConfigureControllers(builder.Services);
        ServiceRegistration.ConfigureLocalization(builder.Services);
        ServiceRegistration.ConfigureAuthentication(builder.Services, builder.Configuration);
        ServiceRegistration.ConfigureOptions(builder.Services, builder.Configuration);
        ServiceRegistration.ConfigureRepositories(builder.Services);
        ServiceRegistration.ConfigureFactories(builder.Services);
        ServiceRegistration.ConfigureApplicationServices(builder.Services);
        ServiceRegistration.ConfigureOCPPServices(builder.Services);
        ServiceRegistration.ConfigureOCPPHandlers(builder.Services);
        ServiceRegistration.ConfigurePayments(builder.Services, builder.Configuration, builder.Environment);

        // Database:Provider = "MySql" (MariaDB / MySQL, e.g. Hostinger) or "SqlServer" (default).
        if (string.Equals(builder.Configuration["Database:Provider"], "MySql", StringComparison.OrdinalIgnoreCase))
            ServiceRegistration.ConfigureDatabaseMySql(builder.Services, builder.Configuration);
        else
            ServiceRegistration.ConfigureDatabaseSqlServer(builder.Services, builder.Configuration);

        ServiceRegistration.ConfigureSwagger(builder.Services);
        SecurityConfiguration.AddSecurity(builder.Services, builder.Configuration, builder.Environment);
        ServiceRegistration.ConfigureAutoMapper(builder.Services);
        ServiceRegistration.ConfigureQuestPDF(builder.Services);
        ServiceRegistration.ConfigureSignalR(builder.Services);
        ServiceRegistration.ConfigureScaleOut(builder.Services, builder.Configuration);
    }

    private static void ConfigureApp(WebApplication app)
    {
        // Configure middleware
        SecurityConfiguration.UseSecurity(app);
        AppConfiguration.ConfigureSwagger(app);
        AppConfiguration.ConfigureRouting(app);
        AppConfiguration.ConfigureLocalization(app);
        AppConfiguration.ConfigureCors(app);
        AppConfiguration.ConfigureCookiePolicy(app);
        AppConfiguration.ConfigureAuth(app);
        // After authentication so the global limit can partition by user.
        SecurityConfiguration.UseRateLimiting(app);

        // After authentication (the log lines carry the caller) and before the endpoints
        // (so handler logs are correlated too).
        app.UseRequestContextLogging();
        LoggingConfiguration.ConfigureRequestLogging(app);

        AppConfiguration.ConfigureEndpoints(app);
        AppConfiguration.ConfigureStaticFiles(app);
        AppConfiguration.ConfigureWebSockets(app);
        AppConfiguration.ConfigureOCPPMiddleware(app);
    }

    private static async Task RunFirstSetup(WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<FirstSetupOptions>>().Value;
        if (!options.Enabled)
            return;

        FirstSetupResult result;
        try
        {
            using var scope = app.Services.CreateScope();
            result = await scope.ServiceProvider.GetRequiredService<FirstSetupJob>().RunAsync();
        }
        catch (Exception ex) when (!options.FailStartupOnError)
        {
            Log.Error(ex, "First setup check could not run");
            return;
        }
        if (result.HasErrors && options.FailStartupOnError)
            throw new InvalidOperationException("First setup check failed, see the log above");
        if (result.Credentials.Count == 0)
        {
            // Fixed passwords from configuration, or rotation off: nothing new to tell anyone.
            if (options.TestUsers.Enabled)
                Log.Information("No test account passwords mailed: no password changed on this start");
            return;
        }

        // Queued: MailDeliveryService sends it once the API runs, so a slow SMTP server never holds the start back.
        var recipient = options.TestUsers.CredentialsRecipient;
        var environment = $"{app.Environment.EnvironmentName} ({app.Configuration["SpaLink"]})";
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMailService>().SendSetupCredentialsMail(recipient, environment, result.Credentials);
            Log.Information("Test account passwords queued for {Recipient}", recipient);
        }
        catch (Exception ex)
        {
            // Never log the passwords: they are only valid until the next start anyway.
            Log.Error(ex, "Could not mail the test account passwords to {Recipient}", recipient);
        }
    }

    private static void SeedDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();

        var mapper = app.Services.GetRequiredService<AutoMapper.IMapper>();
        DatabaseInit.Seed(dbContext, mapper).Wait();
        dbContext.Database.SetCommandTimeout(6000);
    }
}
