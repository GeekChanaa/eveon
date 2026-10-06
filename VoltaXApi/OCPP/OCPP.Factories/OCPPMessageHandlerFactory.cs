using Microsoft.Extensions.DependencyInjection.Extensions;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Factories
{
    public sealed record OcppInboundHandlerRegistration(string ProtocolVersion, string Action, Type HandlerType);

    /// <summary>
    /// Resolves the handler of a charger-initiated CALL by (protocol version, action).
    /// <para>
    /// To support a message for a protocol version, implement <see cref="IOCPPRequestHandler"/> (it gets the raw
    /// JSON payload in <c>msgIn.JsonPayload</c>, writes the response JSON to <c>msgOut.JsonPayload</c> and returns
    /// null or an <see cref="ErrorCodes"/> value) and register it in ServiceRegistration.ConfigureOCPPHandlers:
    /// <code>services.AddOcppInboundHandler&lt;BootNotification16Handler&gt;(OcppProtocols.Ocpp16, "BootNotification");</code>
    /// The handler is resolved from a fresh DI scope for every message. A version is offered to chargers at the
    /// WebSocket handshake as soon as one handler is registered for it (see WebSocketSubProtocolMatcher), so
    /// registering the first 1.6 handler re-enables "ocpp1.6". Outgoing commands are version-agnostic:
    /// IOcppCommandSender.SendRequestAsync takes any request/response types, and
    /// IOcppCommandSender.GetProtocolVersion tells which version a charger speaks.
    /// </para>
    /// </summary>
    public sealed class OcppInboundHandlerRegistry
    {
        private readonly Dictionary<(string Protocol, string Action), Type> _handlers = new();

        public OcppInboundHandlerRegistry(IEnumerable<OcppInboundHandlerRegistration> registrations)
        {
            foreach (var registration in registrations)
            {
                var key = (registration.ProtocolVersion, registration.Action);
                if (_handlers.TryGetValue(key, out var existing) && existing != registration.HandlerType)
                    throw new InvalidOperationException($"Two handlers are registered for {registration.Action} ({registration.ProtocolVersion}).");
                _handlers[key] = registration.HandlerType;
            }

            var registered = _handlers.Keys.Select(k => k.Protocol).Distinct().ToList();
            SupportedProtocols = OcppProtocols.PreferenceOrder.Where(registered.Contains)
                .Concat(registered.Where(p => !OcppProtocols.PreferenceOrder.Contains(p)))
                .ToList();
        }

        /// <summary>Protocol versions with at least one handler, most preferred first.</summary>
        public IReadOnlyList<string> SupportedProtocols { get; }

        public Type? GetHandlerType(string protocolVersion, string action) =>
            _handlers.TryGetValue((protocolVersion, action), out var type) ? type : null;

        /// <summary>Resolves the handler from <paramref name="scopedProvider"/> (the per-message scope), or null when none is registered.</summary>
        public IOCPPRequestHandler? Resolve(IServiceProvider scopedProvider, string protocolVersion, string action)
        {
            var type = GetHandlerType(protocolVersion, action);
            return type == null ? null : (IOCPPRequestHandler)scopedProvider.GetRequiredService(type);
        }
    }

    public static class OcppInboundHandlerServiceCollectionExtensions
    {
        /// <summary>Registers <typeparamref name="THandler"/> (scoped) as the handler of <paramref name="action"/> for <paramref name="protocolVersion"/>.</summary>
        public static IServiceCollection AddOcppInboundHandler<THandler>(this IServiceCollection services, string protocolVersion, string action)
            where THandler : class, IOCPPRequestHandler
        {
            services.TryAddScoped<THandler>();
            services.AddSingleton(new OcppInboundHandlerRegistration(protocolVersion, action, typeof(THandler)));
            return services;
        }
    }
}
