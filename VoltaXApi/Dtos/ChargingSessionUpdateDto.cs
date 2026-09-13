namespace VoltaXApi.Dtos
{
    /// <summary>
    /// DTO sent over SignalR to clients subscribing to charging session updates.
    /// </summary>
    public class ChargingSessionUpdateDto
    {
        public int SessionId { get; set; }
        public string? TransactionUid { get; set; }

        /// <summary>
        /// Current status: "Started", "Charging", "StoppingLowBalance", "Ended"
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Total energy delivered so far (kWh).
        /// </summary>
        public double EnergyKWh { get; set; }

        /// <summary>
        /// Duration of the session in minutes.
        /// </summary>
        public double DurationMinutes { get; set; }

        /// <summary>
        /// Estimated cost so far.
        /// </summary>
        public double CurrentCost { get; set; }

        /// <summary>
        /// Remaining card balance.
        /// </summary>
        public double CardBalance { get; set; }

        /// <summary>
        /// Reason for stopping (only on End).
        /// </summary>
        public string? StopReason { get; set; }

        /// <summary>
        /// Timestamp of this update.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
