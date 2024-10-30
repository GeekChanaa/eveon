using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class HeartBeatHandler : IOCPPRequestHandler
  {
    public string HandleHeartBeat(OCPPMessage msgIn, OCPPMessage msgOut)
    {
        string errorCode = null;

        Logger.LogTrace("Processing heartbeat...");
        HeartbeatResponse heartbeatResponse = new HeartbeatResponse();
        heartbeatResponse.CustomData = new CustomDataType();
        heartbeatResponse.CustomData.VendorId = VendorId;

        heartbeatResponse.CurrentTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        msgOut.JsonPayload = JsonConvert.SerializeObject(heartbeatResponse);
        Logger.LogTrace("Heartbeat => Response serialized");

        WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, null, errorCode);
        return errorCode;
    }
  }
}