using System;
using System.Collections.Generic;
using VoltaXApi.OCPP.Handlers;

namespace VoltaXApi.OCPP.Factories
{
    public class OCPPRequestHandlerFactory
    {
        private readonly Dictionary<string, Lazy<IOCPPRequestHandler>> _handlers;

        public OCPPRequestHandlerFactory(Func<BootNotificationHandler> bootNotificationHandlerFactory,
                                         Func<HeartBeatHandler> heartBeatHandlerFactory,
                                         Func<AuthorizeHandler> authorizeHandlerFactory,
                                         Func<ClearedChargingLimitHandler> clearedChargingLimitHandlerFactory,
                                         Func<DataTransferHandler> dataTransferHandlerFactory,
                                         Func<FirmwareStatusNotificationHandler> firmwareStatusNotificationHandlerFactory,
                                         Func<LogStatusNotificationHandler> logStatusNotificationHandlerFactory,
                                         Func<MeterValuesHandler> meterValuesHandlerFactory,
                                         Func<NotifyChargingLimitHandler> notifyChargingLimitHandlerFactory,
                                         Func<NotifyEVChargingScheduleHandler> notifyEVChargingScheduleHandlerFactory,
                                         Func<ResetHandler> resetHandlerFactory,
                                         Func<SecurityEventNotificationHandler> securityEventNotificationFactory,
                                         Func<StatusNotificationHandler> statusNotificationHandlerFactory,
                                         Func<UnlockConnectorHandler> unlockConnectorHandlerFactory)
        {
            _handlers = new Dictionary<string, Lazy<IOCPPRequestHandler>>
            {
                { "BootNotification", new Lazy<IOCPPRequestHandler>(bootNotificationHandlerFactory) },
                { "Heartbeat", new Lazy<IOCPPRequestHandler>(heartBeatHandlerFactory) },
                { "Authorize", new Lazy<IOCPPRequestHandler>(authorizeHandlerFactory) },
                { "ClearedChargingLimit", new Lazy<IOCPPRequestHandler>(clearedChargingLimitHandlerFactory) },
                { "DataTransfer", new Lazy<IOCPPRequestHandler>(dataTransferHandlerFactory) },
                { "FirmwareStatusNotification", new Lazy<IOCPPRequestHandler>(firmwareStatusNotificationHandlerFactory) },
                { "LogStatusNotification", new Lazy<IOCPPRequestHandler>(logStatusNotificationHandlerFactory) },
                { "MeterValues", new Lazy<IOCPPRequestHandler>(meterValuesHandlerFactory) },
                { "NotifyChargingLimit", new Lazy<IOCPPRequestHandler>(notifyChargingLimitHandlerFactory) },
                { "NotifyEVChargingSchedule", new Lazy<IOCPPRequestHandler>(notifyEVChargingScheduleHandlerFactory) },
                { "Reset", new Lazy<IOCPPRequestHandler>(resetHandlerFactory) },
                { "StatusNotification", new Lazy<IOCPPRequestHandler>(statusNotificationHandlerFactory) },
                { "UnlockConnector", new Lazy<IOCPPRequestHandler>(unlockConnectorHandlerFactory) },
                { "SecurityEventNotification", new Lazy<IOCPPRequestHandler>(securityEventNotificationFactory) },
            };
        }

        public IOCPPRequestHandler GetHandler(string action)
        {
            action = action.Trim();

            var handler = _handlers.TryGetValue(action, out var lazyHandler) ? lazyHandler.Value : null;
            return handler;
        }
    }
}
