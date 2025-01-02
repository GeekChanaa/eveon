namespace VoltaXApi.Dtos
{
    public class CardTransactionDto
    {
        public int ID { get; set; }
        public string? Uid { get; set; }
        public string? ChargePointID { get; set; }
        public int ConnectorID { get; set; }
        public int? StartCardID { get; set; }
        public int? StopCardID { get; set; }
        public DateTime StartTime { get; set; }
        public double MeterStart { get; set; }
        public string? StartResult { get; set; }
        public DateTime? StopTime { get; set; }
        public double? MeterStop { get; set; }
        public string? StopReason { get; set; }
        public double Amount { get; set; }
    }
}