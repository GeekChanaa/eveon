using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.DataProtection;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Configurations;
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
using VoltaXApi.OCPP.Models;
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
        services.AddScoped<VoltaXApi.Authorization.AccessService>();
        services.AddSingleton<VoltaXApi.Authorization.HubConnections>();
        services.AddControllers(options =>
        {
            // Registered by type, not as an instance, so the filter can take ILogger and
            // IHostEnvironment from the container.
            options.Filters.Add<GlobalExceptionFilter>();
            options.Filters.Add<VoltaXApi.Authorization.DashboardAccessFilter>();
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
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        // Protects TOTP secrets and 2FA challenge tokens; keys live in the database so they
        // survive restarts and are shared between instances.
        services.AddDataProtection()
            .SetApplicationName("eveon-api")
            .PersistKeysToDbContext<VoltaXApiDbContext>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                        .GetBytes(configuration.GetSection("AppSettings:Token").Value)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["AppSettings:Issuer"] ?? JwtService.DefaultIssuer,
                    ValidateAudience = true,
                    ValidAudience = configuration["AppSettings:Audience"] ?? JwtService.DefaultAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/notification") || path.StartsWithSegments("/notificationHub") || path.StartsWithSegments("/chargerHub") || path.StartsWithSegments("/chargingSessionHub")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            // Temporary cookie that only lives between the Google redirect and our callback.
            .AddCookie(ExternalAuthDefaults.ExternalCookieScheme, options =>
            {
                options.Cookie.Name = "VoltaX.External";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                options.SignInScheme = ExternalAuthDefaults.ExternalCookieScheme;

                // Default is /signin-google, keep it configurable so it can be aligned
                // with what is declared in the Google Cloud console.
                var callbackPath = configuration["Authentication:Google:CallbackPath"];
                if (!string.IsNullOrWhiteSpace(callbackPath))
                    options.CallbackPath = callbackPath;

                options.SaveTokens = false;
                options.Scope.Add("email");
                options.Scope.Add("profile");

                options.ClaimActions.MapJsonKey(GoogleAuthProvider.PictureClaimType, "picture");
                options.ClaimActions.MapJsonKey(GoogleAuthProvider.EmailVerifiedClaimType, "email_verified");

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.IsEssential = true;
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
        services.Configure<AuthTokenSettings>(configuration.GetSection(AuthTokenSettings.SectionName));
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
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IChargePointModelRepository, ChargePointModelRepository>();
        services.AddScoped<IChargePointBrandRepository, ChargePointBrandRepository>();
        services.AddScoped<IElectricVehicleModelRepository, ElectricVehicleModelRepository>();


    }

    public static void ConfigureFactories(IServiceCollection services)
    {
        services.AddTransient<IMailRequestFactory, MailRequestFactory>();
        services.AddSingleton<IUserClaimsFactory, UserClaimsFactory>();
    }
    
    public static void ConfigureApplicationServices(IServiceCollection services)
    {
        // Register application services
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();
        services.AddScoped<IChargingStationService, ChargingStationService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IPartnerStatisticsService, PartnerStatisticsService>();
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
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartnerAuthService, PartnerAuthService>();
        services.AddScoped<IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<IQRCodeService, QRCodeService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IEmailVerificationGuard, EmailVerificationGuard>();
        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<ISnsService, SnsService>();
        services.AddScoped<IUserInfoDownloadRequestService, UserInfoDownloadRequestService>();
        // GDPR, audit and retention
        services.AddScoped<VoltaXApi.Services.Audit.AuditSaveChangesInterceptor>();
        services.AddSingleton<VoltaXApi.Services.Audit.IAuditLogger, VoltaXApi.Services.Audit.AuditLogger>();
        services.AddScoped<VoltaXApi.Services.Gdpr.IUserDataExportService, VoltaXApi.Services.Gdpr.UserDataExportService>();
        services.AddScoped<VoltaXApi.Services.Gdpr.IAccountDeletionService, VoltaXApi.Services.Gdpr.AccountDeletionService>();
        services.AddHostedService<VoltaXApi.Services.Gdpr.GdprExportWorker>();
        services.AddHostedService<VoltaXApi.Services.Gdpr.AccountDeletionWorker>();
        services.AddHostedService<DataRetentionService>();
        // OCPI 2.2.1 roaming (CPO); endpoints answer 404 unless Ocpi:Enabled.
        VoltaXApi.Ocpi.OcpiRegistration.AddOcpi(services);
        services.AddScoped<IFileManagementService, FileManagementService>();
        // Only implementation until a real payment provider is integrated; it refuses every call outside Development.
        services.AddSingleton<VoltaXApi.Services.Payments.IPaymentCardTokenizer, VoltaXApi.Services.Payments.DevelopmentFakeCardTokenizer>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IWebSocketRequestsHandler, WebSocketRequestsHandler>();
        // Connection-level OCPP objects are singletons; every incoming CALL gets its own DI scope.
        services.AddSingleton<WebSocketHandler>();
        services.AddSingleton<OCPPMessageProcessor>();
        services.AddSingleton<IOcppCommandSender>(sp => sp.GetRequiredService<OCPPMessageProcessor>());
        services.AddSingleton<OcppPendingRequestRegistry>();
        services.AddSingleton<ChargePointStatusManagerService>();
        services.AddScoped<IOCPPTransactionsService, OCPPTransactionsService>();
        services.AddScoped<INoticeService, NoticeService>();
        services.AddScoped<IChargingSessionService, ChargingSessionService>();
        services.AddScoped<ChargingSessionInvoiceGeneratorService>();
        services.AddScoped<IInvoiceGeneratorService<InvoiceData>, RechargeOrderInvoiceGenerator>();
        services.AddSingleton<WebSocketSubProtocolMatcher>();
        
        // Singletons
        services.AddSingleton<WebSocketManagerService>();
        services.AddSingleton<ChargePointConnectivityNotifier>();
        services.AddSingleton<ProvisioningNotifier>();
        services.AddSingleton<ReportCompletionTracker>();
        
        // Creates its own scope per run (IServiceScopeFactory), so it is a plain singleton hosted service.
        services.AddHostedService<CardExpirationWarningService>();
    }
    
    public static void ConfigureOCPPServices(IServiceCollection services)
    {
        // Register OCPP services
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IEVDriverService, EVDriverService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<ProvisioningService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ISecurityService, SecurityService>();
        // Charger PKI: CA, client certificate validation (security profile 3), Kestrel mTLS, expiry monitor.
        VoltaXApi.OCPP.Pki.PkiRegistration.AddChargerPki(services);
        services.AddScoped<ISmartChargingService, SmartChargingService>();
        // Smart charging: stored profiles, load balancing (debounced trigger + 60 s safety pass), strategies
        services.AddSingleton<VoltaXApi.SmartCharging.ChargingProfileReportTracker>();
        services.AddScoped<VoltaXApi.SmartCharging.SmartChargingInboundStore>();
        services.AddScoped<VoltaXApi.SmartCharging.ILoadBalancingService, VoltaXApi.SmartCharging.LoadBalancingService>();
        services.AddSingleton<VoltaXApi.SmartCharging.ILoadBalancingTrigger, VoltaXApi.SmartCharging.LoadBalancingTrigger>();
        services.AddHostedService<VoltaXApi.SmartCharging.LoadBalancingSafetyService>();
        services.AddScoped<VoltaXApi.SmartCharging.IChargingStrategyService, VoltaXApi.SmartCharging.ChargingStrategyService>();
        services.AddScoped<ITransactionsService, TransactionsService>();
        // OCPP 2.0.1 device data (events, monitors, customer information, display messages), CostUpdated, log uploads.
        services.AddScoped<IOcppDeviceDataService, OcppDeviceDataService>();
        services.AddSingleton<ChargerAlarmNotifier>();
        services.AddSingleton<MonitoringReportAssembler>();
        services.AddScoped<IDisplayMessageService, DisplayMessageService>();
        services.AddSingleton<VoltaXApi.Services.ICostCalculator>(VoltaXApi.Services.CostCalculator.Instance);
        services.AddSingleton<ICostUpdatedSender, CostUpdatedSender>();
        services.AddHostedService<CostUpdatedHostedService>();
        services.AddScoped<ILogUploadUrlFactory, LogUploadUrlFactory>();
        services.AddSingleton(sp => ChargerLogStorage.FromConfiguration(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IWebHostEnvironment>()));
    }
    
    public static void ConfigureOCPPHandlers(IServiceCollection services)
    {
        // Inbound (charger -> CSMS) CALL handlers, keyed by protocol version and action; see OcppInboundHandlerRegistry.
        // A protocol version is offered at the WebSocket handshake only once it has handlers here.
        services.AddSingleton<OcppInboundHandlerRegistry>();
        services.AddSingleton<OCPPRequestHandler>();
        // Shared OCPP domain services + reservations, and the OCPP 1.6J handlers (enables "ocpp1.6"); see OCPP/Ocpp16.
        VoltaXApi.OCPP.Ocpp16.Ocpp16Registration.AddOcppSharedDomain(services);
        VoltaXApi.OCPP.Ocpp16.Ocpp16Registration.AddOcpp16(services);

        services.AddOcppInboundHandler<BootNotificationHandler>(OcppProtocols.Ocpp201, "BootNotification");
        services.AddOcppInboundHandler<HeartBeatHandler>(OcppProtocols.Ocpp201, "Heartbeat");
        services.AddOcppInboundHandler<AuthorizeHandler>(OcppProtocols.Ocpp201, "Authorize");
        services.AddOcppInboundHandler<ClearedChargingLimitHandler>(OcppProtocols.Ocpp201, "ClearedChargingLimit");
        services.AddOcppInboundHandler<DataTransferHandler>(OcppProtocols.Ocpp201, "DataTransfer");
        services.AddOcppInboundHandler<FirmwareStatusNotificationHandler>(OcppProtocols.Ocpp201, "FirmwareStatusNotification");
        services.AddOcppInboundHandler<LogStatusNotificationHandler>(OcppProtocols.Ocpp201, "LogStatusNotification");
        services.AddOcppInboundHandler<MeterValuesHandler>(OcppProtocols.Ocpp201, "MeterValues");
        services.AddOcppInboundHandler<NotifyChargingLimitHandler>(OcppProtocols.Ocpp201, "NotifyChargingLimit");
        services.AddOcppInboundHandler<NotifyEVChargingScheduleHandler>(OcppProtocols.Ocpp201, "NotifyEVChargingSchedule");
        services.AddOcppInboundHandler<NotifyEVChargingNeedsHandler>(OcppProtocols.Ocpp201, "NotifyEVChargingNeeds");
        services.AddOcppInboundHandler<ReportChargingProfilesHandler>(OcppProtocols.Ocpp201, "ReportChargingProfiles");
        services.AddOcppInboundHandler<SecurityEventNotificationHandler>(OcppProtocols.Ocpp201, "SecurityEventNotification");
        services.AddOcppInboundHandler<SignCertificateHandler>(OcppProtocols.Ocpp201, "SignCertificate");
        services.AddOcppInboundHandler<GetCertificateStatusHandler>(OcppProtocols.Ocpp201, "GetCertificateStatus");
        services.AddOcppInboundHandler<Get15118EVCertificateHandler>(OcppProtocols.Ocpp201, "Get15118EVCertificate");
        services.AddOcppInboundHandler<StatusNotificationHandler>(OcppProtocols.Ocpp201, "StatusNotification");
        services.AddOcppInboundHandler<TransactionEventHandler>(OcppProtocols.Ocpp201, "TransactionEvent");
        services.AddOcppInboundHandler<NotifyReportHandler>(OcppProtocols.Ocpp201, "NotifyReport");
        services.AddSingleton<ConnectorReportBuffer>();
        services.AddOcppInboundHandler<NotifyEventHandler>(OcppProtocols.Ocpp201, "NotifyEvent");
        services.AddOcppInboundHandler<NotifyMonitoringReportHandler>(OcppProtocols.Ocpp201, "NotifyMonitoringReport");
        services.AddOcppInboundHandler<NotifyCustomerInformationHandler>(OcppProtocols.Ocpp201, "NotifyCustomerInformation");
        services.AddOcppInboundHandler<NotifyDisplayMessagesHandler>(OcppProtocols.Ocpp201, "NotifyDisplayMessages");
    }
    
    public static void ConfigureDatabaseMySql(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoltaXApiDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MariaDbServerVersion("10.6.15");
            
            options.UseMySql(connectionString, serverVersion)
                .AddInterceptors(serviceProvider.GetRequiredService<VoltaXApi.Services.Audit.AuditSaveChangesInterceptor>());
        });
    }

    public static void ConfigureDatabaseSqlServer(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoltaXApiDbContext>((serviceProvider, options) =>
                 options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                     .AddInterceptors(serviceProvider.GetRequiredService<VoltaXApi.Services.Audit.AuditSaveChangesInterceptor>()));
    }
    
    public static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
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
        services.AddSignalR(options => options.AddFilter<VoltaXApi.Authorization.AuthorizedHubFilter>());
    }

    /// <summary>Multi-instance support (Redis backplane, charger registry, command routing); see docs/scale-out.md.</summary>
    public static void ConfigureScaleOut(IServiceCollection services, IConfiguration configuration)
    {
        VoltaXApi.ScaleOut.ScaleOutRegistration.AddScaleOut(services, configuration);
    }
}
