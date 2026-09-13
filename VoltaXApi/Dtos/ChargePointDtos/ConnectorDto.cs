using VoltaXApi.OCPP.Messages;

namespace VoltaxApi.Dtos
{
    /// <summary>
    /// Per-connector info returned to the mobile app so it can decide which
    /// connector a user's cable is plugged into (auto-start vs. picker popup).
    /// Enums serialize as string names via the global JsonStringEnumConverter.
    /// </summary>
    public class ConnectorDto
    {
        // DB id of the connector (use this to start a session on a specific connector).
        public int ID { get; set; }

        // OCPP connector number on the charge point (1-based). Nullable if not provisioned.
        public int? ConnectorID { get; set; }

        // EVSE id the connector belongs to.
        public int EvseID { get; set; }

        // Display label (mirrors what ChargingPorts carried: the connector type name).
        public string? Name { get; set; }

        // Live connector status. No status row for the connector => Disconnected.
        public ConnectorStatusEnumType Status { get; set; }

        // Physical connector type, e.g. cType2, cCCS2, cG105. Nullable if unknown.
        public ConnectorEnumType? ConnectorType { get; set; }

        // Max power in kW, nullable.
        public double? PowerKw { get; set; }

        // Flat fee for using this connector, nullable.
        public double? FlatFee { get; set; }

        // Price per minute for using this connector, nullable.
        public double? PricePerMinute { get; set; }

        // Price per idle minute for using this connector, nullable.
        public double? PricePerIdleMinute { get; set; }
    }
}
