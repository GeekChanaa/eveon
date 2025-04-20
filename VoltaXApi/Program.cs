using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Data.Seeders;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.Helpers;
using System.Text.Json.Serialization;
using VoltaXApi.Mappers;
using Microsoft.Extensions.FileProviders;
using VoltaXApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.Settings;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.Hubs;
using VoltaXApi.OCPP.Factories;
using AutoMapper;
using QuestPDF.Infrastructure;
using VoltaxApi.Helpers;
using VoltaXApi.Filters;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5000); 
// });
builder.Logging.ClearProviders(); 
builder.Logging.AddConsole();     
builder.Logging.AddDebug(); 
builder.Logging.SetMinimumLevel(LogLevel.Warning); 


// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Use Serilog as the logging provider
builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new GlobalExceptionFilter());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

builder.Services.AddSignalR();

builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[] { new CultureInfo("en-US") };
        options.DefaultRequestCulture = new RequestCulture("en-US");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
    });

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                                    .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];
                                // If the request is for our hub...
                                var path = context.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken) &&
                                    (path.StartsWithSegments("/notification")))
                                {
                        // Read the token out of the query string
                        context.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });

builder.Services.Configure<SupportEmails>(builder.Configuration.GetSection("SupportEmails"));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<ChargingSessionInvoiceGeneratorService>();

builder.Services.AddScoped<IOcppComponentRepository, OcppComponentRepository>();
builder.Services.AddScoped<IRatingReportRepository, RatingReportRepository>();
builder.Services.AddScoped<IOcppVariableRepository, OcppVariableRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IOCPPTransactionsService, OCPPTransactionsService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<IChargePointRepository, ChargePointRepository>();
builder.Services.AddScoped<IChargeTagRepository, ChargeTagRepository>();
builder.Services.AddScoped<IChargingStationRepository, ChargingStationRepository>();
builder.Services.AddScoped<ISystemReportCommentRepository, SystemReportCommentRepository>();
builder.Services.AddScoped<IUserInfoDownloadRequestRepository, UserInfoDownloadRequestRepository>();
builder.Services.AddScoped<IChargingStationImageRepository, ChargingStationImageRepository>();
builder.Services.AddScoped<IChargingStationService, ChargingStationService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IChargingStationImageService, ChargingStationImageService>();
builder.Services.AddScoped<IChargePointService, ChargePointService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IConnectorService, ConnectorService>();
builder.Services.AddScoped<IConnectorStatusService, ConnectorStatusService>();
builder.Services.AddScoped<ISystemReportService, SystemReportService>();
builder.Services.AddScoped<IOcppComponentsVariablesService, OcppComponentsVariablesService>();



builder.Services.AddSingleton<IHostedService>(serviceProvider =>
{
    // Create a scope
    using var scope = serviceProvider.CreateScope();
    
    // Resolve the service from the scope
    return scope.ServiceProvider.GetRequiredService<CardExpirationWarningService>();
});

// Register the actual service as scoped
builder.Services.AddScoped<CardExpirationWarningService>();
builder.Services.AddScoped<ISystemReportRepository, SystemReportRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IConnectorRepository, ConnectorRepository>();
builder.Services.AddScoped<IConnectorStatusRepository, ConnectorStatusRepository>();
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
builder.Services.AddScoped<IDebitCardRepository, DebitCardRepository>();
builder.Services.AddScoped<IMessageLogRepository, MessageLogRepository>();
builder.Services.AddScoped<IChargePointUptimeRepository, ChargePointUptimeRepository>();
builder.Services.AddScoped<IConnectorUptimeRepository, ConnectorUptimeRepository>();
builder.Services.AddScoped<IChargingSessionRepository, ChargingSessionRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IChargePointModelRepository, ChargePointModelRepository>();
builder.Services.AddScoped<IOCPPConfigurationComponentRepository, OCPPConfigurationComponentRepository>();
builder.Services.AddScoped<IOCPPConfigurationVariableRepository, OCPPConfigurationVariableRepository>();

builder.Services.AddScoped<ICardExpirationNotificationRepository, CardExpirationNotificationRepository>();

// OCPP Local List 
builder.Services.AddScoped<IOCPPLocalListItemRepository, OCPPLocalListItemRepository>();
builder.Services.AddScoped<IOCPPLocalListVersionRepository, OCPPLocalListVersionRepository>();

builder.Services.AddScoped<IOCPPConfigurationItemRepository,OCPPConfigurationItemRepository>();

builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentReplyRepository, CommentReplyRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportReplyRepository, ReportReplyRepository>();

builder.Services.AddScoped<OCPPRequestHandler>();
builder.Services.AddScoped<OCPPRequestHandlerFactory>();

builder.Services.AddScoped<BootNotificationHandler>();
builder.Services.AddScoped<HeartBeatHandler>();
builder.Services.AddScoped<AuthorizeHandler>();
builder.Services.AddScoped<ClearedChargingLimitHandler>();
builder.Services.AddScoped<DataTransferHandler>();
builder.Services.AddScoped<FirmwareStatusNotificationHandler>();
builder.Services.AddScoped<LogStatusNotificationHandler>();
builder.Services.AddScoped<MeterValuesHandler>();
builder.Services.AddScoped<NotifyChargingLimitHandler>();
builder.Services.AddScoped<NotifyEVChargingScheduleHandler>();
builder.Services.AddScoped<ResetHandler>();
builder.Services.AddScoped<SecurityEventNotificationHandler>();
builder.Services.AddScoped<StatusNotificationHandler>();
builder.Services.AddScoped<UnlockConnectorHandler>();
builder.Services.AddScoped<TransactionEventHandler>();
builder.Services.AddScoped<NotifyReportHandler>();

builder.Services.AddScoped<Func<BootNotificationHandler>>(sp => () => sp.GetService<BootNotificationHandler>());
builder.Services.AddScoped<Func<HeartBeatHandler>>(sp => () => sp.GetService<HeartBeatHandler>());
builder.Services.AddScoped<Func<AuthorizeHandler>>(sp => () => sp.GetService<AuthorizeHandler>());
builder.Services.AddScoped<Func<ClearedChargingLimitHandler>>(sp => () => sp.GetService<ClearedChargingLimitHandler>());
builder.Services.AddScoped<Func<DataTransferHandler>>(sp => () => sp.GetService<DataTransferHandler>());
builder.Services.AddScoped<Func<FirmwareStatusNotificationHandler>>(sp => () => sp.GetService<FirmwareStatusNotificationHandler>());
builder.Services.AddScoped<Func<LogStatusNotificationHandler>>(sp => () => sp.GetService<LogStatusNotificationHandler>());
builder.Services.AddScoped<Func<MeterValuesHandler>>(sp => () => sp.GetService<MeterValuesHandler>());
builder.Services.AddScoped<Func<NotifyChargingLimitHandler>>(sp => () => sp.GetService<NotifyChargingLimitHandler>());
builder.Services.AddScoped<Func<NotifyEVChargingScheduleHandler>>(sp => () => sp.GetService<NotifyEVChargingScheduleHandler>());
builder.Services.AddScoped<Func<ResetHandler>>(sp => () => sp.GetService<ResetHandler>());
builder.Services.AddScoped<Func<SecurityEventNotificationHandler>>(sp => () => sp.GetService<SecurityEventNotificationHandler>());
builder.Services.AddScoped<Func<StatusNotificationHandler>>(sp => () => sp.GetService<StatusNotificationHandler>());
builder.Services.AddScoped<Func<UnlockConnectorHandler>>(sp => () => sp.GetService<UnlockConnectorHandler>());
builder.Services.AddScoped<Func<TransactionEventHandler>>(sp => () => sp.GetService<TransactionEventHandler>());
builder.Services.AddScoped<Func<NotifyReportHandler>>(sp => () => sp.GetService<NotifyReportHandler>());


builder.Services.Configure<CompanyInformations>(builder.Configuration.GetSection("CompanyInformations"));

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IInvoiceGeneratorService<InvoiceData>, RechargeOrderInvoiceGenerator>();
builder.Services.AddScoped<WebSocketSubProtocolMatcher>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IUserInfoDownloadRequestService, UserInfoDownloadRequestService>();
builder.Services.AddScoped<IFileManagementService, FileManagementService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

builder.Services.AddScoped<IWebSocketRequestsHandler, WebSocketRequestsHandler>();
builder.Services.AddScoped<WebSocketHandler>();
builder.Services.AddScoped<OCPPMessageProcessor>();


// Registration of the OCPP Services : 
builder.Services.AddScoped<IConfigurationService,ConfigurationService>();
builder.Services.AddScoped<IEVDriverService,EVDriverService>();
builder.Services.AddScoped<IMonitoringService,MonitoringService>();
builder.Services.AddScoped<IReportingService,ReportingService>();
builder.Services.AddScoped<ISecurityService,SecurityService>();
builder.Services.AddScoped<ISmartChargingService,SmartChargingService>();
builder.Services.AddScoped<ITransactionsService,TransactionsService>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddAutoMapper(typeof(SystemReportMapperProfile));
builder.Services.AddAutoMapper(typeof(ChargingSessionProfile));
builder.Services.AddAutoMapper(typeof(ChargingStationProfile));
builder.Services.AddAutoMapper(typeof(UserProfile));
builder.Services.AddAutoMapper(typeof(MessageLogMapperProfile));
builder.Services.AddAutoMapper(typeof(RatingMapperProfile));
builder.Services.AddAutoMapper(typeof(PartnerMapperProfile));
builder.Services.AddAutoMapper(typeof(TransactionMapperProfile));
builder.Services.AddDbContext<VoltaXApiDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddDbContext<VoltaXApiDbContext>((serviceProvider, options) =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//     var serverVersion = new MariaDbServerVersion("10.6.15");

//     options.UseMySql(connectionString, serverVersion);
// });

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<CardExpirationSettings>(builder.Configuration.GetSection("CardExpirationSettings"));

builder.Services.AddSingleton(resolver =>
{
    var configuration = resolver.GetRequiredService<IConfiguration>();
    var globalConfig = new GlobalConfigurations();
    configuration.GetSection("GlobalConfigurations").Bind(globalConfig);
    return globalConfig;
});
builder.Services.AddSingleton<WebSocketManagerService>();
builder.Services.AddSingleton<ChargePointStatusManagerService>();
builder.Services.AddSingleton<RequestQueueManagerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            builder => builder
                .SetIsOriginAllowed(_ => true) 
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
        );
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}





// app.UseHttpsRedirection();
app.UseRouting();
app.UseRequestLocalization();
app.UseCors("CorsPolicy");
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "OCPP",
        pattern: "ocpp/{controller=OCPP}/{action=Index}/{id?}");
    endpoints.MapHub<ChargerHub>("/chargerHub");
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "/StaticFiles",
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();

    var mapper = app.Services.GetRequiredService<IMapper>();
    // GlobalSeeder.Seed(dbContext, mapper).Wait();
    
    //SeedingNotificationTypes.Initialize(app.Services);
    dbContext.Database.SetCommandTimeout(6000); 
}

var webSocketOptions = new WebSocketOptions()
{
    ReceiveBufferSize = 8 * 1024
};

app.UseWebSockets(webSocketOptions);

app.UseOCPPMiddleware();
app.Run();
