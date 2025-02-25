using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargePointListDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        public int ChargingStationID { get; set; }
        public string Name { get; set; }
        public string ChargingStationName { get; set; }
        public string SerialNumber { get; set; }
        public string Make { get; set; }
        public bool? ShowOnMap { get; set; } = true;
        public bool? HasChargeCable { get; set; } = true;
        public string? ModelName { get; set; }
        public string? ModelImage { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public ChargePointCategoryEnum Category { get; set; }
        public ChargePointStatusEnum Status { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }
        public string? PartnerName { get; set; }
        public virtual ICollection<ConnectorListDto>? Connectors { get; set; }
    }
}