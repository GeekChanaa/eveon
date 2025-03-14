namespace VoltaXApi.Dtos
{
    public class TransactionListDto
    {
        public string? Uid { get; set; }
        public string? ChargePointID { get; set; }
        public int ConnectorID { get; set; }
        public int ChargingSessionID { get; set; }
        public string? StartTagId { get; set; }
        public DateTime StartTime { get; set; }
        public double MeterStart { get; set; }
        public string? StartResult { get; set; }
        public string? StopTagId { get; set; }
        public DateTime? StopTime { get; set; }
        public double? MeterStop { get; set; }
        public string? StopReason { get; set; }
        public double Amount { get; set; }
        public int? CardID { get; set; }
    }
}
