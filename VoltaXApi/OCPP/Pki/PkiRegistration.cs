using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace VoltaXApi.OCPP.Pki
{
    public static class PkiRegistration
    {
        /// <summary>Charger CA, security profile enforcement helpers, Kestrel mTLS, forwarded client certificates and the expiry monitor.</summary>
        public static void AddChargerPki(IServiceCollection services)
        {
            services.AddSingleton(sp => OcppTransportSettings.From(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IHostEnvironment>()));
            services.AddSingleton<IChargerCertificateAuthority, ChargerCertificateAuthority>();
            services.AddSingleton<IChargerClientCertificateValidator, ChargerClientCertificateValidator>();
            services.AddScoped<IChargerCertificateService, ChargerCertificateService>();
            services.AddTransient<IStartupFilter, OcppForwardedClientCertificateStartupFilter>();
            services.AddSingleton<IConfigureOptions<KestrelServerOptions>, OcppKestrelTls>();
            services.AddHostedService<ChargerCertificateExpiryMonitor>();
        }
    }
}
