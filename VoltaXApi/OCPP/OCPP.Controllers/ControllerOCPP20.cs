using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Models;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.OCPP.Handlers;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP20 : ControllerBase
    {
        public const string VendorId = "VoltaX Charging";
        private readonly ChargePointStatus chargePointStatus;

        public ControllerOCPP20(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus cpStatus) :
            base(config, loggerFactory, cpStatus)
        {
            Logger = loggerFactory.CreateLogger(typeof(ControllerOCPP20));
            chargePointStatus = cpStatus;
        }

        
    }
}
