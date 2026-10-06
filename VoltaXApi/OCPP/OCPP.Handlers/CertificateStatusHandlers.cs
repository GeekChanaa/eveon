using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// GetCertificateStatus (M06): the CSMS has no OCSP client, so it always answers Failed with a statusInfo
    /// saying so; the charger then relies on its cached status or its own policy.
    /// </summary>
    public class GetCertificateStatusHandler : IOCPPRequestHandler
    {
        private readonly ILogger<GetCertificateStatusHandler> _logger;
        private readonly IMessageLogRepository _msgLogRepo;

        public GetCertificateStatusHandler(ILogger<GetCertificateStatusHandler> logger, IMessageLogRepository messageLogRepository)
        {
            _logger = logger;
            _msgLogRepo = messageLogRepository;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            try
            {
                var request = JsonConvert.DeserializeObject<GetCertificateStatusRequest>(msgIn.JsonPayload ?? string.Empty);
                _logger.LogWarning("GetCertificateStatus => {ChargePointId} asked for OCSP status of serial {Serial} at {Responder}; answered Failed (no OCSP support)",
                    chargePointStatus.Id, request?.OcspRequestData?.SerialNumber, request?.OcspRequestData?.ResponderURL);
                var response = new GetCertificateStatusResponse
                {
                    Status = GetCertificateStatusEnumType.Failed,
                    StatusInfo = new StatusInfoType { ReasonCode = "NoOCSP", AdditionalInfo = "The CSMS does not query OCSP responders." }
                };
                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (JsonException exp)
            {
                _logger.LogError(exp, "GetCertificateStatus => Invalid payload from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.FormationViolation;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, "Failed", errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }

    /// <summary>Get15118EVCertificate (M01/M02): ISO 15118 Plug &amp; Charge is not supported; always Failed.</summary>
    public class Get15118EVCertificateHandler : IOCPPRequestHandler
    {
        private readonly ILogger<Get15118EVCertificateHandler> _logger;
        private readonly IMessageLogRepository _msgLogRepo;

        public Get15118EVCertificateHandler(ILogger<Get15118EVCertificateHandler> logger, IMessageLogRepository messageLogRepository)
        {
            _logger = logger;
            _msgLogRepo = messageLogRepository;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            try
            {
                var request = JsonConvert.DeserializeObject<Get15118EVCertificateRequest>(msgIn.JsonPayload ?? string.Empty);
                _logger.LogWarning("Get15118EVCertificate => {ChargePointId} ({Schema}, {Action}) answered Failed: Plug & Charge is not supported",
                    chargePointStatus.Id, request?.Iso15118SchemaVersion, request?.Action);
                var response = new Get15118EVCertificateResponse
                {
                    Status = Iso15118EVCertificateStatusEnumType.Failed,
                    ExiResponse = "",
                    StatusInfo = new StatusInfoType { ReasonCode = "NotSupported", AdditionalInfo = "ISO 15118 Plug & Charge is not supported by this CSMS." }
                };
                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (JsonException exp)
            {
                _logger.LogError(exp, "Get15118EVCertificate => Invalid payload from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.FormationViolation;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, "Failed", errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
