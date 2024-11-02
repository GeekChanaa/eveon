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
        private readonly OCPPRequestHandler _reqHandler;
        private readonly ChargePointStatus chargePointStatus;

        public ControllerOCPP20(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus cpStatus, OCPPRequestHandler requestHandler) :
            base(config, loggerFactory, cpStatus)
        {
            Logger = loggerFactory.CreateLogger(typeof(ControllerOCPP20));
            chargePointStatus = cpStatus;
        }
        
        

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public async Task ProcessAnswer(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            await this._reqHandler.ProcessRequest(msgIn,chargePointStatus);
        }

        
    }
}
