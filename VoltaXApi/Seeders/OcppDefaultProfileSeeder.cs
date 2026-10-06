using Microsoft.EntityFrameworkCore;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data.Seeders;

/// <summary>
/// The default OCPP 2.0.1 provisioning profile (QR start, prepaid wallet, no RFID, no free charging).
/// Seeded once: if the table has ever held a row, even a deleted one, the admin's edits win.
/// </summary>
public static class OcppDefaultProfileSeeder
{
    public static async Task Seed(VoltaXApiDbContext db)
    {
        if (await db.OcppDefaultVariables.IgnoreQueryFilters().AnyAsync())
            return;

        db.OcppDefaultVariables.AddRange(Defaults());
        await db.SaveChangesAsync();
    }

    public static List<OcppDefaultVariable> Defaults() => new()
    {
        Row("Transactions", "TxCtrlr", null, "TxStartPoint", null, "PowerPathClosed", true, 10,
            "A transaction only exists once the user is authorized and the cable is plugged in. Prevents empty transactions."),
        Row("Transactions", "TxCtrlr", null, "TxStopPoint", null, "EVConnected,Authorized", true, 20,
            "The transaction ends on unplug or on a stop request."),
        Row("Transactions", "TxCtrlr", null, "EVConnectionTimeOut", null, "90", true, 30,
            "Seconds to wait for a cable after authorization."),
        Row("Transactions", "TxCtrlr", null, "StopTxOnEVSideDisconnect", null, "true", true, 40,
            "Stop the transaction when the cable is pulled from the car."),
        Row("Transactions", "TxCtrlr", null, "StopTxOnInvalidId", null, "true", true, 50,
            "Stop if the CSMS reports the idToken as invalid."),
        Row("Transactions", "TxCtrlr", null, "MaxEnergyOnInvalidId", null, "0", true, 60,
            "Energy allowed before stopping when the idToken turns out invalid."),
        Row("Transactions", "TxCtrlr", null, "TxBeforeAcceptedEnabled", null, "false", true, 70,
            "Do not allow transactions before the CSMS accepts the BootNotification."),
        Row("Authorization", "AuthCtrlr", null, "Enabled", null, "true", true, 80,
            "Authorization is required. false means free vend."),
        Row("Authorization", "AuthCtrlr", null, "AuthorizeRemoteStart", null, "false", true, 90,
            "The CSMS already checked the wallet before RequestStartTransaction; saves a round trip."),
        Row("Authorization", "AuthCtrlr", null, "LocalAuthorizeOffline", null, "false", true, 100,
            "Never start from the cache or local list while offline."),
        Row("Authorization", "AuthCtrlr", null, "LocalPreAuthorize", null, "false", true, 110,
            "Never start from the cache before asking the CSMS."),
        Row("Authorization", "AuthCtrlr", null, "OfflineTxForUnknownIdEnabled", null, "false", true, 120,
            "Unknown idTokens cannot charge while offline (otherwise: free energy)."),
        Row("Authorization", "AuthCacheCtrlr", null, "Enabled", null, "false", true, 130,
            "No authorization cache on the charger."),
        Row("Authorization", "LocalAuthListCtrlr", null, "Enabled", null, "false", true, 140,
            "No local whitelist, unless fleet or staff cards must work offline."),
        Row("Communication", "OCPPCommCtrlr", null, "HeartbeatInterval", null, "300", true, 150,
            "Seconds between heartbeats."),
        Row("Communication", "OCPPCommCtrlr", null, "WebSocketPingInterval", null, "60", true, 160,
            "WebSocket ping frequency; detects dead sockets faster than heartbeats."),
        Row("Communication", "OCPPCommCtrlr", null, "OfflineThreshold", null, "300", true, 170,
            "Seconds offline after which the charger sends a full status update on reconnect."),
        Row("Communication", "OCPPCommCtrlr", null, "QueueAllMessages", null, "true", true, 180,
            "Queue every message while offline, not only transaction messages."),
        Row("Communication", "OCPPCommCtrlr", null, "MessageAttempts", "TransactionEvent", "3", true, 190,
            "Retries for TransactionEvent messages."),
        Row("Communication", "OCPPCommCtrlr", null, "MessageAttemptInterval", "TransactionEvent", "60", true, 200,
            "Seconds between TransactionEvent retries."),
        Row("Communication", "OCPPCommCtrlr", null, "MessageTimeout", "Default", "30", true, 210,
            "How long the charger waits for CSMS responses."),
        Row("Communication", "OCPPCommCtrlr", null, "UnlockOnEVSideDisconnect", null, "true", true, 220,
            "Release the socket lock when the car-side cable is removed."),
        Row("Communication", "OCPPCommCtrlr", null, "ResetRetries", null, "2", true, 230,
            "Retries after a failed reset."),
        Row("Metering", "SampledDataCtrlr", null, "TxUpdatedInterval", null, "60", true, 240,
            "Seconds between meter values during a transaction; how fast an empty wallet is noticed."),
        Row("Metering", "SampledDataCtrlr", null, "TxUpdatedMeasurands", null, "Energy.Active.Import.Register,Power.Active.Import,Current.Import,Voltage,SoC", true, 250,
            "Values sent during charging."),
        Row("Metering", "SampledDataCtrlr", null, "TxStartedMeasurands", null, "Energy.Active.Import.Register", true, 260,
            "Values sent at the start of the transaction."),
        Row("Metering", "SampledDataCtrlr", null, "TxEndedMeasurands", null, "Energy.Active.Import.Register", true, 270,
            "Values sent at the end of the transaction."),
        Row("Metering", "SampledDataCtrlr", null, "TxEndedInterval", null, "0", true, 280,
            "0 = only the final value in the Ended event."),
        Row("Metering", "SampledDataCtrlr", null, "SignReadings", null, "false", true, 290,
            "Signed meter values; enable for calibration-law compliance."),
        Row("Metering", "AlignedDataCtrlr", null, "Interval", null, "900", true, 300,
            "Clock-aligned readings for site energy reporting (seconds)."),
        Row("Metering", "AlignedDataCtrlr", null, "Measurands", null, "Energy.Active.Import.Register", true, 310,
            "Values sent with clock-aligned readings."),
        Row("Metering", "AlignedDataCtrlr", null, "SendDuringIdle", null, "false", true, 320,
            "Do not send aligned readings when no transaction is running."),
        Row("Clock", "ClockCtrlr", null, "TimeSource", null, "NTP,Heartbeat", true, 330,
            "NTP first, heartbeat as fallback."),
        Row("Clock", "ClockCtrlr", null, "NtpServerUri", null, "pool.ntp.org", true, 340,
            "NTP server to use."),
        Row("Clock", "ClockCtrlr", null, "TimeZone", null, "Africa/Casablanca", true, 350,
            "Local time for the charger display only; OCPP messages are always UTC."),
        Row("Tariff & cost", "TariffCostCtrlr", "Tariff", "Enabled", null, "true", true, 360,
            "Show the tariff on the charger screen."),
        Row("Tariff & cost", "TariffCostCtrlr", "Cost", "Enabled", null, "true", true, 370,
            "Show the running cost on the charger screen."),
        Row("Tariff & cost", "TariffCostCtrlr", null, "Currency", null, "MAD", true, 380,
            "Currency used on the charger screen."),
        Row("Smart charging", "SmartChargingCtrlr", null, "Enabled", null, "true", true, 390,
            "Needed for power limits, load balancing and capping energy to the wallet."),
        Row("Security", "SecurityCtrlr", null, "SecurityProfile", null, "2", false, 400,
            "TLS + basic auth. Can only be raised, never lowered: test on one charger before enabling."),
    };

    private static OcppDefaultVariable Row(string group, string component, string? componentInstance,
        string variable, string? variableInstance, string value, bool enabled, int sortOrder, string description) => new()
    {
        GroupName = group,
        ComponentName = component,
        ComponentInstance = componentInstance,
        VariableName = variable,
        VariableInstance = variableInstance,
        AttributeType = AttributeEnumType.Actual,
        Value = value,
        Enabled = enabled,
        SortOrder = sortOrder,
        Description = description
    };
}
