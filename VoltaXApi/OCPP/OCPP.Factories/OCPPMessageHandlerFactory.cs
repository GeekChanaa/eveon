using System;
using System.Collections.Generic;
using VoltaXApi.OCPP.Handlers;

namespace VoltaXApi.OCPP.Factories
{
    public class OCPPRequestHandlerFactory
    {
        private readonly Dictionary<string, Lazy<IOCPPRequestHandler>> _handlers;

        public OCPPRequestHandlerFactory(Func<BootNotificationHandler> bootNotificationHandlerFactory,
                                         Func<HeartBeatHandler> heartbeatHandlerFactory,
                                         Func<AuthorizeHandler> authorizeHandlerFactory)
        {
            _handlers = new Dictionary<string, Lazy<IOCPPRequestHandler>>
            {
                { "BootNotification", new Lazy<IOCPPRequestHandler>(bootNotificationHandlerFactory) },
                { "Heartbeat", new Lazy<IOCPPRequestHandler>(heartbeatHandlerFactory) },
                { "Authorize", new Lazy<IOCPPRequestHandler>(authorizeHandlerFactory) },
            };
        }

        public IOCPPRequestHandler GetHandler(string action)
        {
            return _handlers.TryGetValue(action, out var lazyHandler) ? lazyHandler.Value : null;
        }
    }
}
