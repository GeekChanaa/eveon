using System.Collections.Concurrent;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Pki;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// SignCertificate (A02/A03): the CSR is checked at once (signature, key, CN = identity) and answered
    /// Accepted/Rejected; an accepted CSR is signed by the charger CA and delivered with CertificateSigned
    /// in the background, after this CALL is answered.
    /// </summary>
    public class SignCertificateHandler : IOCPPRequestHandler
    {
        // Lets the CALLRESULT of SignCertificate reach the charger before the CertificateSigned CALL.
        private static readonly TimeSpan SendDelay = TimeSpan.FromSeconds(2);
        // One accepted CSR per charger per minute: every accepted CSR costs a signature and a stored certificate.
        private static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(1);
        private static readonly ConcurrentDictionary<string, DateTime> LastAccepted = new();

        private readonly ILogger<SignCertificateHandler> _logger;
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly IChargerCertificateAuthority _ca;
        private readonly IServiceScopeFactory _scopeFactory;

        public SignCertificateHandler(ILogger<SignCertificateHandler> logger, IMessageLogRepository messageLogRepository,
            IChargerCertificateAuthority ca, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _msgLogRepo = messageLogRepository;
            _ca = ca;
            _scopeFactory = scopeFactory;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            string? result = null;
            try
            {
                var request = JsonConvert.DeserializeObject<SignCertificateRequest>(msgIn.JsonPayload ?? string.Empty);
                if (request == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    var certificateType = request.certificateType ?? CertificateSigningUseEnumType.ChargingStationCertificate;
                    var (status, reasonCode, info) = await Decide(request, certificateType, chargePointStatus.Id);
                    result = status.ToString();

                    var response = new SignCertificateResponse
                    {
                        status = status,
                        statusInfo = reasonCode == null ? null : new StatusInfoType { ReasonCode = reasonCode, AdditionalInfo = info! }
                    };
                    msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);

                    if (status == GenericStatusEnumType.Accepted)
                    {
                        var chargePointId = chargePointStatus.Id;
                        var csr = request.csr;
                        OcppBackgroundCommand.Run(_scopeFactory, _logger, "CertificateSigned", chargePointId, async services =>
                        {
                            await Task.Delay(SendDelay);
                            await services.GetRequiredService<IChargerCertificateService>().IssueAndSendAsync(chargePointId, csr, certificateType);
                        });
                    }
                    else
                    {
                        _logger.LogWarning("SignCertificate => {ChargePointId} {Type} rejected: {Reason}", chargePointStatus.Id, certificateType, info);
                    }
                }
            }
            catch (JsonException exp)
            {
                _logger.LogError(exp, "SignCertificate => Invalid payload from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.FormationViolation;
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "SignCertificate => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, result!, errorCode!, msgIn, msgOut);
            return errorCode!;
        }

        private async Task<(GenericStatusEnumType Status, string? ReasonCode, string? Info)> Decide(SignCertificateRequest request,
            CertificateSigningUseEnumType certificateType, string chargePointId)
        {
            if (certificateType == CertificateSigningUseEnumType.V2GCertificate)
                return (GenericStatusEnumType.Rejected, "NotSupported", "V2G (ISO 15118) certificates are not issued by this CSMS.");
            if (await _ca.GetCaAsync() == null)
                return (GenericStatusEnumType.Rejected, "NoCA", "The CSMS has no charger CA configured.");
            var validation = ChargerPkiCrypto.ValidateCsr(request.csr, chargePointId);
            if (!validation.IsValid)
                return (GenericStatusEnumType.Rejected, "InvalidCSR", validation.Error);

            var now = DateTime.UtcNow;
            var last = LastAccepted.GetOrAdd(chargePointId, DateTime.MinValue);
            if (now - last < MinInterval || !LastAccepted.TryUpdate(chargePointId, now, last))
                return (GenericStatusEnumType.Rejected, "TooFrequent", "A certificate was requested less than a minute ago.");
            return (GenericStatusEnumType.Accepted, null, null);
        }
    }
}
