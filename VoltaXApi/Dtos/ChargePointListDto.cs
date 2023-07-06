

namespace VoltaXApi.Dtos
{
    public class ChargePointListDto
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string Make { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }
        public virtual ICollection<ConnectorListDto>? Connectors { get; set; }
    }
}