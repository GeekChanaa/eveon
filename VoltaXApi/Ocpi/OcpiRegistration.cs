using VoltaXApi.Ocpi.Services;
using VoltaXApi.Services;

namespace VoltaXApi.Ocpi
{
    public static class OcpiRegistration
    {
        public static IServiceCollection AddOcpi(this IServiceCollection services)
        {
            services.AddOptions<OcpiOptions>().BindConfiguration(OcpiOptions.SectionName);
            services.AddHttpClient(OcpiClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<OcpiClient>();
            services.AddScoped<OcpiPartyService>();
            services.AddScoped<OcpiCredentialsService>();
            services.AddScoped<OcpiDataService>();
            services.AddScoped<OcpiTokenStore>();
            services.AddScoped<OcpiCommandService>();
            services.AddScoped<IOcpiCommandExecutor, OcppOcpiCommandExecutor>();
            services.AddScoped<IExternalTokenAuthorizer, OcpiTokenAuthorizer>();
            services.AddScoped<IRoamingTransactionObserver, OcpiTransactionObserver>();
            services.AddHostedService<OcpiPushWorker>();
            return services;
        }
    }
}
