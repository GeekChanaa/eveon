using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class DataTransferHandler : IOCPPRequestHandler
  {
      public string HandleDataTransfer(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            DataTransferResponse dataTransferResponse = new DataTransferResponse();

            bool msgWritten = false;

            try
            {
                Logger.LogTrace("Processing data transfer...");
                DataTransferRequest dataTransferRequest = JsonConvert.DeserializeObject<DataTransferRequest>(msgIn.JsonPayload);
                Logger.LogTrace("DataTransfer => Message deserialized");

                if (ChargePointStatus != null)
                {
                    // Known charge station
                    msgWritten = WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, string.Format("VendorId={0} / MessageId={1} / Data={2}", dataTransferRequest.VendorId, dataTransferRequest.MessageId, dataTransferRequest.Data), errorCode);
                    dataTransferResponse.Status = DataTransferStatusEnumType.Accepted;
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(dataTransferResponse);
                Logger.LogTrace("DataTransfer => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DataTransfer => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            if (!msgWritten)
            {
                WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, null, errorCode);
            }
            return errorCode;
        }
  }
}