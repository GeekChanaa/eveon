
using VoltaXApi.Models;

namespace VoltaxApi.Dtos
{
    public class ChargePointDetailsForMobileDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        public int ChargingStationID { get; set; }
        public string? SerialNumber { get; set; }
        public bool? ShowOnMap { get; set; } = true;
        public bool? HasChargeCable { get; set; } = true;
        public ChargePointStatusEnum Status { get; set; }
        public string? Comment { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        // Kept for backward compatibility with clients still reading the old
        // string list. New clients should use Connectors instead.
        public List<string> ChargingPorts { get; set; }
        // Structured per-connector list carrying live status + type + power.
        public List<ConnectorDto> Connectors { get; set; }
        public ChargePointCategoryEnum Category { get; set; }
    }
}