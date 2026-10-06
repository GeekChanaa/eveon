namespace VoltaXApi.Models
{
    // Stored as int; names follow OCPP 2.0.1 (1.6 maps ChargingStationMaxProfile to ChargePointMaxProfile).
    public enum ChargingProfilePurposeEnum
    {
        ChargingStationMaxProfile,
        TxDefaultProfile,
        TxProfile,
        ChargingStationExternalConstraints
    }

    public enum ChargingProfileKindEnum
    {
        Absolute,
        Recurring,
        Relative
    }

    public enum ChargingProfileRecurrencyEnum
    {
        Daily,
        Weekly
    }

    public enum ChargingRateUnitEnum
    {
        A,
        W
    }

    public enum ChargingProfileSourceEnum
    {
        Csms,
        LoadBalancer,
        Strategy,
        ChargerReported
    }

    public enum ChargingProfileStatusEnum
    {
        Pending,
        Accepted,
        Rejected,
        Cleared
    }

    public enum LoadBalancingStrategyEnum
    {
        EqualShare,
        FirstComeFirstServed
    }
}
