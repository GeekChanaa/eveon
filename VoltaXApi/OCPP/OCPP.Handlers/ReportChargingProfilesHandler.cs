using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// ReportChargingProfiles (2.0.1), answer to GetChargingProfiles: parts are collected by requestId until tbc is
    /// false, then the whole report is reconciled with the stored profiles.
    /// </summary>
    public class ReportChargingProfilesHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ChargingProfileReportTracker _tracker;
        private readonly ISmartChargingService _smartCharging;
        private readonly ILogger<ReportChargingProfilesHandler> _logger;

        public ReportChargingProfilesHandler(
            IMessageLogRepository messageLogRepository,
            ChargingProfileReportTracker tracker,
            ISmartChargingService smartCharging,
            ILogger<ReportChargingProfilesHandler> logger)
        {
            _msgLogRepo = messageLogRepository;
            _tracker = tracker;
            _smartCharging = smartCharging;
            _logger = logger;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            string? result = null;
            int? evseId = null;
            var response = new ReportChargingProfilesResponse { CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId } };

            try
            {
                var request = JsonConvert.DeserializeObject<ReportChargingProfilesRequest>(msgIn.JsonPayload ?? string.Empty);
                if (request == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    evseId = request.EvseId;
                    result = $"Request {request.RequestId}: {request.ChargingProfile?.Count ?? 0} profile(s){(request.Tbc == true ? ", more to come" : "")}";
                    var (complete, scope, parts) = _tracker.AddPart(chargePointStatus.Id, request);
                    if (complete)
                        await _smartCharging.ReconcileReportedProfilesAsync(chargePointStatus.Id, scope, parts);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "ReportChargingProfiles => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, evseId, msgIn.Action, result!, errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
