using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OCPP.Core.Server;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json.Serialization;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Filters;
using VoltaXApi.Helpers;
using VoltaXApi.Hubs;
using VoltaXApi.Mappers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using VoltaXApi.Settings;
using VoltaxApi.Helpers;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using VoltaXApi.Factories;

namespace VoltaXApi;

public static class ServiceRegistration
{
    public static void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(new GlobalExceptionFilter());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }
    
    public static void ConfigureLocalization(IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[] { new CultureInfo("en-US") };
            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });
    }
    
    public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                        .GetBytes(configuration.GetSection("AppSettings:Token").Value)),
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
    }
    
    public static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupportEmails>(configuration.GetSection("SupportEmails"));
        services.Configure<CompanyInformations>(configuration.GetSection("CompanyInformations"));
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        services.Configure<CardExpirationSettings>(configuration.GetSection("CardExpirationSettings"));
        services.Configure<CardConfigurationSettings>(configuration.GetSection("CardConfigurationSettings"));
        services.Configure<AwsSnsOptions>(configuration.GetSection("AwsSns"));
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
        });
        
        services.AddSingleton(resolver =>
        {
            var globalConfig = new GlobalConfigurations();
            configuration.GetSection("GlobalConfigurations").Bind(globalConfig);
            return globalConfig;
        });
    }

    public static void ConfigureRepositories(IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register all repositories
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
        services.AddScoped<IOcppComponentRepository, OcppComponentRepository>();
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IRatingReportRepository, RatingReportRepository>();
        services.AddScoped<IOcppVariableRepository, OcppVariableRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<IChargePointRepository, ChargePointRepository>();
        services.AddScoped<IChargeTagRepository, ChargeTagRepository>();
        services.AddScoped<IChargingStationRepository, ChargingStationRepository>();
        services.AddScoped<ISystemReportCommentRepository, SystemReportCommentRepository>();
        services.AddScoped<IUserInfoDownloadRequestRepository, UserInfoDownloadRequestRepository>();
        services.AddScoped<IChargingStationImageRepository, ChargingStationImageRepository>();
        services.AddScoped<ISystemReportRepository, SystemReportRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IConnectorRepository, ConnectorRepository>();
        services.AddScoped<IConnectorStatusRepository, ConnectorStatusRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
        services.AddScoped<IDebitCardRepository, DebitCardRepository>();
        services.AddScoped<IMessageLogRepository, MessageLogRepository>();
        services.AddScoped<IChargePointUptimeRepository, ChargePointUptimeRepository>();
        services.AddScoped<IConnectorUptimeRepository, ConnectorUptimeRepository>();
        services.AddScoped<IChargingSessionRepository, ChargingSessionRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IChargePointModelRepository, ChargePointModelRepository>();
        services.AddScoped<IOCPPConfigurationComponentRepository, OCPPConfigurationComponentRepository>();
        services.AddScoped<IOCPPConfigurationVariableRepository, OCPPConfigurationVariableRepository>();
        services.AddScoped<ICardExpirationNotificationRepository, CardExpirationNotificationRepository>();
        services.AddScoped<IOCPPLocalListItemRepository, OCPPLocalListItemRepository>();
        services.AddScoped<IOCPPLocalListVersionRepository, OCPPLocalListVersionRepository>();
        services.AddScoped<IOCPPConfigurationItemRepository, OCPPConfigurationItemRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentReplyRepository, CommentReplyRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportReplyRepository, ReportReplyRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IChargePointModelRepository, ChargePointModelRepository>();
        services.AddScoped<IChargePointBrandRepository, ChargePointBrandRepository>();
        services.AddScoped<IElectricVehicleModelRepository, ElectricVehicleModelRepository>();


    }

    public static void ConfigureFactories(IServiceCollection services)
    {
        services.AddTransient<IMailRequestFactory, MailRequestFactory>();
    }
    
    public static void ConfigureApplicationServices(IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IChargingStationService, ChargingStationService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IChargingStationImageService, ChargingStationImageService>();
        services.AddScoped<IChargePointService, ChargePointService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IConnectorService, ConnectorService>();
        services.AddScoped<IConnectorStatusService, ConnectorStatusService>();
        services.AddScoped<ISystemReportService, SystemReportService>();
        services.AddScoped<IOcppComponentsVariablesService, OcppComponentsVariablesService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartnerAuthService, PartnerAuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<ISnsService, SnsService>();
        services.AddScoped<IUserInfoDownloadRequestService, UserInfoDownloadRequestService>();
        services.AddScoped<IFileManagementService, FileManagementService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IWebSocketRequestsHandler, WebSocketRequestsHandler>();
        services.AddScoped<WebSocketHandler>();
        services.AddScoped<OCPPMessageProcessor>();
        services.AddScoped<IOCPPTransactionsService, OCPPTransactionsService>();
        services.AddScoped<INoticeService, NoticeService>();
        services.AddScoped<ChargingSessionInvoiceGeneratorService>();
        services.AddScoped<IInvoiceGeneratorService<InvoiceData>, RechargeOrderInvoiceGenerator>();
        services.AddScoped<WebSocketSubProtocolMatcher>();
        
        // Singletons
        services.AddSingleton<WebSocketManagerService>();
        services.AddSingleton<ChargePointStatusManagerService>();
        services.AddSingleton<RequestQueueManagerService>();
        
        // Register hosted services
        services.AddSingleton<IHostedService>(serviceProvider =>
        {
            // Create a scope
            using var scope = serviceProvider.CreateScope();
            
            // Resolve the service from the scope
            return scope.ServiceProvider.GetRequiredService<CardExpirationWarningService>();
        });
        
        // Register the actual service as scoped
        services.AddScoped<CardExpirationWarningService>();
    }
    
    public static void ConfigureOCPPServices(IServiceCollection services)
    {
        // Register OCPP services
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IEVDriverService, EVDriverService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<ISmartChargingService, SmartChargingService>();
        services.AddScoped<ITransactionsService, TransactionsService>();
    }
    
    public static void ConfigureOCPPHandlers(IServiceCollection services)
    {
        // Register OCPP handlers
        services.AddScoped<OCPPRequestHandler>();
        services.AddScoped<OCPPRequestHandlerFactory>();
        
        // Register specific handlers
        services.AddScoped<BootNotificationHandler>();
        services.AddScoped<HeartBeatHandler>();
        services.AddScoped<AuthorizeHandler>();
        services.AddScoped<ClearedChargingLimitHandler>();
        services.AddScoped<DataTransferHandler>();
        services.AddScoped<FirmwareStatusNotificationHandler>();
        services.AddScoped<LogStatusNotificationHandler>();
        services.AddScoped<MeterValuesHandler>();
        services.AddScoped<NotifyChargingLimitHandler>();
        services.AddScoped<NotifyEVChargingScheduleHandler>();
        services.AddScoped<ResetHandler>();
        services.AddScoped<SecurityEventNotificationHandler>();
        services.AddScoped<StatusNotificationHandler>();
        services.AddScoped<UnlockConnectorHandler>();
        services.AddScoped<TransactionEventHandler>();
        services.AddScoped<NotifyReportHandler>();
        
        // Register handler factories
        services.AddScoped<Func<BootNotificationHandler>>(sp => () => sp.GetService<BootNotificationHandler>());
        services.AddScoped<Func<HeartBeatHandler>>(sp => () => sp.GetService<HeartBeatHandler>());
        services.AddScoped<Func<AuthorizeHandler>>(sp => () => sp.GetService<AuthorizeHandler>());
        services.AddScoped<Func<ClearedChargingLimitHandler>>(sp => () => sp.GetService<ClearedChargingLimitHandler>());
        services.AddScoped<Func<DataTransferHandler>>(sp => () => sp.GetService<DataTransferHandler>());
        services.AddScoped<Func<FirmwareStatusNotificationHandler>>(sp => () => sp.GetService<FirmwareStatusNotificationHandler>());
        services.AddScoped<Func<LogStatusNotificationHandler>>(sp => () => sp.GetService<LogStatusNotificationHandler>());
        services.AddScoped<Func<MeterValuesHandler>>(sp => () => sp.GetService<MeterValuesHandler>());
        services.AddScoped<Func<NotifyChargingLimitHandler>>(sp => () => sp.GetService<NotifyChargingLimitHandler>());
        services.AddScoped<Func<NotifyEVChargingScheduleHandler>>(sp => () => sp.GetService<NotifyEVChargingScheduleHandler>());
        services.AddScoped<Func<ResetHandler>>(sp => () => sp.GetService<ResetHandler>());
        services.AddScoped<Func<SecurityEventNotificationHandler>>(sp => () => sp.GetService<SecurityEventNotificationHandler>());
        services.AddScoped<Func<StatusNotificationHandler>>(sp => () => sp.GetService<StatusNotificationHandler>());
        services.AddScoped<Func<UnlockConnectorHandler>>(sp => () => sp.GetService<UnlockConnectorHandler>());
        services.AddScoped<Func<TransactionEventHandler>>(sp => () => sp.GetService<TransactionEventHandler>());
        services.AddScoped<Func<NotifyReportHandler>>(sp => () => sp.GetService<NotifyReportHandler>());
    }
    
    public static void ConfigureDatabaseMySql(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoltaXApiDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MariaDbServerVersion("10.6.15");
            
            options.UseMySql(connectionString, serverVersion);
        });
    }

    public static void ConfigureDatabaseSqlServer(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoltaXApiDbContext>(options =>
                 options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    }
    
    public static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
    
    public static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
            );
        });
    }
    
    public static void ConfigureAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddAutoMapper(typeof(SystemReportMapperProfile));
        services.AddAutoMapper(typeof(ChargingSessionProfile));
        services.AddAutoMapper(typeof(ChargingStationProfile));
        services.AddAutoMapper(typeof(UserProfile));
        services.AddAutoMapper(typeof(MessageLogMapperProfile));
        services.AddAutoMapper(typeof(RatingMapperProfile));
        services.AddAutoMapper(typeof(PartnerMapperProfile));
        services.AddAutoMapper(typeof(TransactionMapperProfile));
    }
    
    public static void ConfigureQuestPDF(IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public static void ConfigureSignalR(IServiceCollection services)
    {
        services.AddSignalR();
    }
}