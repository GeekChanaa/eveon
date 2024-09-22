namespace VoltaXApi.Dtos
{
    public class CommentDisplayDto
    {
        public int ID { get; set; }
        public int? UserID { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; }
        public string? UserName { get; set; }
        public string? ChargingStationName { get; set; }
        public string? ChargePointName { get; set; }
        public string? ConnectorName { get; set; }
        public int? ChargingStationID { get; set; }
        public int? ChargePointID { get; set; }
        public int? ConnectorID { get; set; }
        public DateTime CommentTime { get; set; }
    }
}