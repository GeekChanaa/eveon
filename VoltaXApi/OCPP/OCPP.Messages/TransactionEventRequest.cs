using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
    public class TransactionEventRequest
    {
        [Required]
        public CustomDataType CustomData { get; set; }

        [Required]
        public TransactionEventEnumType EventType { get; set; }

        [Required]
        public TriggerReasonEnumType TriggerReason { get; set; }

        [Required]
        public EVSEType EVSE { get; set; }

        [Required]
        public IdTokenType IdToken { get; set; }

        public string Timestamp { get; set; }

        public ChargingStateEnumType? ChargingState { get; set; }

        public ReasonEnumType? Reason { get; set; }

        public List<MeterValueType> MeterValues { get; set; }

        public TransactionType TransactionInfo { get; set; }
    }

    public enum ChargingStateEnumType
    {
        Charging,
        EVConnected,
        SuspendedEV,
        SuspendedEVSE,
        Idle
    }

    public enum ReasonEnumType
    {
        DeAuthorized,
        EmergencyStop,
        EnergyLimitReached,
        EVDisconnected,
        GroundFault,
        ImmediateReset,
        Local,
        LocalOutOfCredit,
        MasterPass,
        Other,
        OvercurrentFault,
        PowerLoss,
        PowerQuality,
        Reboot,
        Remote,
        SOCLimitReached,
        StoppedByEV,
        TimeLimitReached,
        Timeout
    }

    public enum TransactionEventEnumType
    {
        Ended,
        Started,
        Updated
    }

    public enum TriggerReasonEnumType
    {
        Authorized,
        CablePluggedIn,
        ChargingRateChanged,
        ChargingStateChanged,
        Deauthorized,
        EnergyLimitReached,
        EVCommunicationLost,
        EVConnectTimeout,
        MeterValueClock,
        MeterValuePeriodic,
        TimeLimitReached,
        Trigger,
        UnlockCommand,
        StopAuthorized,
        EVDeparted,
        EVDetected,
        RemoteStop,
        RemoteStart,
        AbnormalCondition,
        SignedDataReceived,
        ResetCommand
    }

    public class TransactionType
    {
        public CustomDataType CustomData { get; set; }

        [MaxLength(36)]
        [Required]
        public string TransactionId { get; set; }

        public ChargingStateEnumType ChargingState { get; set; }

        public int? TimeSpentCharging { get; set; }

        public ReasonEnumType StoppedReason { get; set; }

        public int? RemoteStartId { get; set; }
    }

}