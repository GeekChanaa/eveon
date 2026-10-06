using VoltaXApi.OCPP.Factories;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Ocpp16
{
    public static class Ocpp16Registration
    {
        /// <summary>Domain services shared by the OCPP 1.6 and 2.0.1 handlers, and reservations (both versions).</summary>
        public static IServiceCollection AddOcppSharedDomain(this IServiceCollection services)
        {
            services.AddScoped<IdTokenAuthorizationService>();
            services.AddScoped<ChargePointBootService>();
            services.AddScoped<ReservationService>();
            services.AddScoped<Ocpp16CommandService>();
            services.AddHostedService<ReservationExpiryService>();
            services.AddOcppInboundHandler<ReservationStatusUpdateHandler>(OcppProtocols.Ocpp201, "ReservationStatusUpdate");
            return services;
        }

        /// <summary>OCPP 1.6J inbound handlers; registering them makes the server accept the "ocpp1.6" sub-protocol.</summary>
        public static IServiceCollection AddOcpp16(this IServiceCollection services)
        {
            services.AddScoped<Ocpp16ConnectorResolver>();
            services.AddOcppInboundHandler<BootNotification16Handler>(OcppProtocols.Ocpp16, "BootNotification");
            services.AddOcppInboundHandler<Heartbeat16Handler>(OcppProtocols.Ocpp16, "Heartbeat");
            services.AddOcppInboundHandler<StatusNotification16Handler>(OcppProtocols.Ocpp16, "StatusNotification");
            services.AddOcppInboundHandler<Authorize16Handler>(OcppProtocols.Ocpp16, "Authorize");
            services.AddOcppInboundHandler<StartTransaction16Handler>(OcppProtocols.Ocpp16, "StartTransaction");
            services.AddOcppInboundHandler<StopTransaction16Handler>(OcppProtocols.Ocpp16, "StopTransaction");
            services.AddOcppInboundHandler<MeterValues16Handler>(OcppProtocols.Ocpp16, "MeterValues");
            services.AddOcppInboundHandler<DataTransfer16Handler>(OcppProtocols.Ocpp16, "DataTransfer");
            services.AddOcppInboundHandler<DiagnosticsStatusNotification16Handler>(OcppProtocols.Ocpp16, "DiagnosticsStatusNotification");
            services.AddOcppInboundHandler<FirmwareStatusNotification16Handler>(OcppProtocols.Ocpp16, "FirmwareStatusNotification");
            return services;
        }
    }
}
