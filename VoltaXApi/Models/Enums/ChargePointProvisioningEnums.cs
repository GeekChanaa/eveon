namespace VoltaXApi.Models
{
    public enum ChargePointProvisioningStatusEnum
    {
        Provisioned = 0,
        // Settings were sent and at least one needs a reboot to take effect.
        AwaitingReboot = 1
    }

    public enum ChargePointProvisioningMethodEnum
    {
        Automatic = 0,
        Manual = 1,
        // Accepted as-is, without sending any setting.
        Skipped = 2,
        // Charge points that existed before provisioning was introduced.
        Legacy = 3
    }
}
