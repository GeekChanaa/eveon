using Microsoft.EntityFrameworkCore;
using Serilog;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Models;

namespace VoltaXApi.Configurations;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // builder.WebHost.ConfigureKestrel(options =>
        // {
        //     options.ListenAnyIP(5000);
        // });
        
        // Configure logging
        ConfigureLogging(builder);
        
        // Configure services
        ConfigureServices(builder);
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline
        ConfigureApp(app);
        
        // Run database seeding
        //SeedDatabase(app);
        
        app.Run();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        // Use Serilog as the logging provider
        // builder.Host.UseSerilog();
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
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

        // ServiceRegistration.ConfigureDatabaseMySql(builder.Services, builder.Configuration);
        ServiceRegistration.ConfigureDatabaseSqlServer(builder.Services, builder.Configuration);
        
        ServiceRegistration.ConfigureSwagger(builder.Services);
        ServiceRegistration.ConfigureCors(builder.Services);
        ServiceRegistration.ConfigureAutoMapper(builder.Services);
        ServiceRegistration.ConfigureQuestPDF(builder.Services);
        ServiceRegistration.ConfigureSignalR(builder.Services);
    }
    
    private static void ConfigureApp(WebApplication app)
    {
        // Configure middleware
        AppConfiguration.ConfigureSwagger(app);
        AppConfiguration.ConfigureRouting(app);
        AppConfiguration.ConfigureLocalization(app);
        AppConfiguration.ConfigureCors(app);
        AppConfiguration.ConfigureCookiePolicy(app);
        AppConfiguration.ConfigureAuth(app);
        AppConfiguration.ConfigureEndpoints(app);
        AppConfiguration.ConfigureStaticFiles(app);
        AppConfiguration.ConfigureWebSockets(app);
        AppConfiguration.ConfigureOCPPMiddleware(app);
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