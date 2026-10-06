using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class DataTransferHandler : IOCPPRequestHandler
    {
        private readonly ILogger _logger;
        private readonly IMessageLogRepository _msgLogRepo;
        
        public DataTransferHandler(
          ILoggerFactory loggerFactory,
          IMessageLogRepository messageLogRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(DataTransferHandler));
            _msgLogRepo = messageLogRepository;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string errorCode = null;
            DataTransferResponse dataTransferResponse = new DataTransferResponse();

            bool msgWritten = false;

            try
            {
                _logger.LogTrace("Processing data transfer...");
                DataTransferRequest dataTransferRequest = JsonConvert.DeserializeObject<DataTransferRequest>(msgIn.JsonPayload);
                _logger.LogTrace("DataTransfer => Message deserialized");

                if (chargePointStatus != null)
                {
                    // Known charge station
                    msgWritten = await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, string.Format("VendorId={0} / MessageId={1} / Data={2}", dataTransferRequest.VendorId, dataTransferRequest.MessageId, dataTransferRequest.Data), errorCode, msgIn, msgOut);
                    dataTransferResponse.Status = DataTransferStatusEnumType.Accepted;
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(dataTransferResponse, OCPPMessageFactory.DefaultSettings);
                _logger.LogTrace("DataTransfer => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "DataTransfer => Exception processing request from {ChargePointId}", chargePointStatus?.Id);
                errorCode = ErrorCodes.InternalError;
            }

            if (!msgWritten)
            {
                await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, null, errorCode, msgIn, msgOut);
            }
            return errorCode;
        }
    }
}