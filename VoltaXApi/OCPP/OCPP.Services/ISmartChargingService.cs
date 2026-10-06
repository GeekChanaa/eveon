using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Smart charging for OCPP 2.0.1 and 1.6 chargers (routed by the charger's live protocol). Every command waits for
    /// the charger's answer and stores it on the ChargingProfiles rows; transport failures (TimeoutException,
    /// WebSocketNotFoundException, OcppCallErrorException) are recorded on the profile, then rethrown.
    /// Invalid input throws <see cref="VoltaXApi.Exceptions.ValidationException"/>.
    /// </summary>
    public interface ISmartChargingService
    {
        Task<ChargingProfileCommandResult> SendChargingProfileAsync(string chargePointId, ChargingProfileInputDto input,
            ChargingProfileSourceEnum source, int? userId, int? chargingStrategyId = null, CancellationToken cancellationToken = default);

        /// <summary>Raw 2.0.1 SetChargingProfile.req (generic OCPP request tool); also sent to 1.6 chargers once converted.</summary>
        Task<ChargingProfileCommandResult> SetChargingProfileAsync(string chargePointId, SetChargingProfileRequest request, int? userId,
            CancellationToken cancellationToken = default);

        Task<ClearChargingProfileResult> ClearStoredChargingProfileAsync(string chargePointId, int chargingProfileId,
            CancellationToken cancellationToken = default);

        /// <summary>Raw 2.0.1 ClearChargingProfile.req (by id or criteria); also sent to 1.6 chargers once converted.</summary>
        Task<ClearChargingProfileResult> ClearChargingProfileAsync(string chargePointId, ClearChargingProfileRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>2.0.1 only: asks the charger to report its profiles (ReportChargingProfiles), which are then reconciled with the database.</summary>
        Task<GetChargingProfilesResult> GetChargingProfilesAsync(string chargePointId, GetChargingProfilesRequest request,
            CancellationToken cancellationToken = default);

        Task<CompositeScheduleResult> GetCompositeScheduleAsync(string chargePointId, CompositeScheduleRequestDto request,
            CancellationToken cancellationToken = default);

        Task<List<ChargingProfileDto>> GetStoredProfilesAsync(int chargePointDbId, bool includeCleared, CancellationToken cancellationToken = default);

        /// <summary>Applies a complete ReportChargingProfiles report: known profiles are confirmed, unknown ones stored as ChargerReported.</summary>
        Task ReconcileReportedProfilesAsync(string chargePointId, ChargingProfileReportScope? scope,
            IReadOnlyList<ReportChargingProfilesRequest> parts, CancellationToken cancellationToken = default);
    }
}
