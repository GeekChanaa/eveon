using VoltaXApi.OCPP.Messages;

namespace VoltaX.E2E;

/// <summary>The price of every seeded connector (VAT included, as every EVEON price).</summary>
public static class Tariff
{
    public const decimal PricePerKWh = 2.50m;
    public const decimal PricePerMinute = 0.10m;
    public const decimal FlatFee = 1.00m;
    public const decimal PricePerIdleMinute = 0.50m;
    public const decimal VatRate = 0.2m;
    public const int GracePeriodSeconds = 300;

    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}

public enum SessionExpectation
{
    /// <summary>Exactly one session, matching the one transaction the charger reported.</summary>
    One,
    /// <summary>One session per transaction the charger reported (and nothing else).</summary>
    PerTransaction,
    /// <summary>No session at all on the charge point.</summary>
    None
}

/// <summary>
/// One EVEON_SIM preset and what VoltaX must show after it ran: the sessions, their stop reason, whether the energy
/// must match what the charger reported, whether an idle fee is allowed. The card is the preset's own (one user per
/// card, so the user's sessions are the card's).
/// </summary>
public sealed record ScenarioSpec(
    string Preset,
    string Slug,
    string? IdTag,
    SessionExpectation Sessions = SessionExpectation.One,
    decimal Balance = 200m,
    ReasonEnumType[]? Reasons = null,
    bool EnergyFromReport = true,
    bool IdleAllowed = true,
    bool RemoteStartStop = false)
{
    /// <summary>The card this run uses; remote runs get one per protocol version (the CSMS resolves it from the user).</summary>
    public string? CardFor(string version) => RemoteStartStop ? $"{IdTag}-{VersionTag(version)}" : IdTag;

    public string UserFor(string version) =>
        $"e2e-{(RemoteStartStop ? $"{Slug}-{VersionTag(version)}" : Slug)}@example.com".ToLowerInvariant();

    public string ChargePointFor(string version) => $"E2E-{VersionTag(version)}-{Slug}".ToUpperInvariant();

    public static string VersionTag(string version) => version == "1.6" ? "16" : "201";
}

public static class Scenarios
{
    private static ReasonEnumType[] R(params ReasonEnumType[] reasons) => reasons;

    /// <summary>
    /// The presets that run against a real CSMS. Not here: the ones that need a CSMS command the runner does not send
    /// (UpdateFirmware), never end their transaction (power cut forgotten: the stale sweeper closes it after hours),
    /// or exercise the network and not the billing (reconnect storm, zombie charger, boot pending).
    /// </summary>
    public static readonly ScenarioSpec[] All =
    {
        new("Happy path", "HAPPY", "RFID-HAPPY"),
        new("Offline mid-session", "OFFLINE", "RFID-OFFLINE", Reasons: R(ReasonEnumType.Local)),
        new("Duplicate storm", "DUP", "RFID-DUP", SessionExpectation.PerTransaction),
        new("Lost responses", "LOSSY", "RFID-LOSSY"),
        new("Balance exhaustion", "LOWBAL", "RFID-LOWBALANCE", Balance: 15m, Reasons: R(ReasonEnumType.Remote)),
        new("Fault mid-session", "FAULT", "RFID-FAULT", Reasons: R(ReasonEnumType.Other, ReasonEnumType.GroundFault), IdleAllowed: false),
        new("EV unplugged mid-session", "UNPLUG", "RFID-UNPLUG", Reasons: R(ReasonEnumType.EVDisconnected)),
        new("Suspended by EV and by charger", "SUSPEND", "RFID-SUSPEND", Reasons: R(ReasonEnumType.Local)),
        new("Power loss while offline", "PWRLOSS", "RFID-POWERLOSS", Reasons: R(ReasonEnumType.PowerLoss), IdleAllowed: false),
        new("Power cut, transaction resumed", "RESUME", "RFID-RESUME", Reasons: R(ReasonEnumType.Local)),
        new("Connection drop mid-session", "RECONN", "RFID-RECONNECT", Reasons: R(ReasonEnumType.Local)),
        new("Remote start and stop", "REMOTE", "RFID-REMOTE", Reasons: R(ReasonEnumType.Remote), RemoteStartStop: true),
        new("Unknown card refused", "UNKNOWN", null, SessionExpectation.None),
        new("Full car left plugged in", "IDLE", "RFID-IDLE"),
        new("Bad meter data", "BADMETER", "RFID-BADMETER", EnergyFromReport: false),
    };
}
