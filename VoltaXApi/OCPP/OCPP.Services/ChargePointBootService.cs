using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Services
{
    public sealed record BootDecision(RegistrationStatusEnumType Status, int Interval);

    /// <summary>
    /// BootNotification acceptance for every OCPP version: unknown charge points are Rejected, never-provisioned
    /// ones Pending, provisioned ones Accepted (see ChargePointRegistration).
    /// </summary>
    public class ChargePointBootService
    {
        public const int AcceptedHeartbeatInterval = 300;
        // A pending charger retries its BootNotification after this many seconds.
        public const int PendingRetryInterval = 60;
        // An unknown charger retries after this many seconds (it may be registered meanwhile).
        public const int RejectedRetryInterval = 300;

        private readonly IChargePointRepository _chargePointRepository;
        private readonly IChargePointService _chargePointService;
        private readonly VoltaXApiDbContext _db;
        private readonly ProvisioningNotifier _provisioningNotifier;
        private readonly ILogger<ChargePointBootService> _logger;

        public ChargePointBootService(IChargePointRepository chargePointRepository, IChargePointService chargePointService,
            VoltaXApiDbContext db, ProvisioningNotifier provisioningNotifier, ILogger<ChargePointBootService> logger)
        {
            _chargePointRepository = chargePointRepository;
            _chargePointService = chargePointService;
            _db = db;
            _provisioningNotifier = provisioningNotifier;
            _logger = logger;
        }

        /// <param name="bootInfo">Charger identity (model, vendor, serial) in 2.0.1 form; 1.6 handlers map theirs to it.</param>
        public async Task<BootDecision> RegisterBootAsync(ChargePointStatus chargePointStatus, BootNotificationRequest bootInfo)
        {
            var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
            if (chargePoint == null)
            {
                _logger.LogWarning("BootNotification => Unknown charge point {ChargePointId} rejected", chargePointStatus.Id);
                return new BootDecision(RegistrationStatusEnumType.Rejected, RejectedRetryInterval);
            }

            await _chargePointService.SetBootNotificationInfo(chargePointStatus, bootInfo);

            // Never-provisioned charge points stay Pending: the CSMS may configure them, but they cannot
            // start transactions until an operator applies (or skips) the settings profile.
            var provisioning = await _db.ChargePointProvisionings.FirstOrDefaultAsync(p => p.ChargePointID == chargePoint.ID);
            if (provisioning == null)
            {
                _provisioningNotifier.AwaitingProvisioning(chargePointStatus.Id);
                return new BootDecision(RegistrationStatusEnumType.Pending, PendingRetryInterval);
            }

            if (provisioning.Status == ChargePointProvisioningStatusEnum.AwaitingReboot)
            {
                // This boot is the reboot the provisioning asked for.
                provisioning.Status = ChargePointProvisioningStatusEnum.Provisioned;
                await _db.SaveChangesAsync();
            }
            return new BootDecision(RegistrationStatusEnumType.Accepted, AcceptedHeartbeatInterval);
        }
    }
}
