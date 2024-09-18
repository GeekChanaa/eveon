using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargePointDisplayDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        public int ChargingStationID { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string? Make { get; set; }
        public ChargePointStatusEnum Status { get; set; }
        public string? Comment { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientCertThumb { get; set; }
        public ChargePointCategoryEnum Category { get; set; }
        public string ChargingStationName { get; set; }
    }
}