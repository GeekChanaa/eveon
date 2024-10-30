using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Models;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP20 : ControllerBase
    {
        public const string VendorId = "VoltaX Charging";

        public ControllerOCPP20(IConfiguration config, ILoggerFactory loggerFactory, ChargePointStatus chargePointStatus) :
            base(config, loggerFactory, chargePointStatus)
        {
            Logger = loggerFactory.CreateLogger(typeof(ControllerOCPP20));
        }
        
        

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public void ProcessAnswer(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            switch (msgOut.Action)
            {
                case "Reset":
                    HandleReset(msgIn, msgOut);
                    break;

                case "UnlockConnector":
                    HandleUnlockConnector(msgIn, msgOut);
                    break;

                default:
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, "Unknown answer");
                    break;
            }
        }

        
    }
}
