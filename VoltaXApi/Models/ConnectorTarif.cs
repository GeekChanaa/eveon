namespace VoltaXApi.Models
{
    public class ConnectorTarif : IEntity
    {
        public int ID { get; set; }
        
        public int ConnectorID { get; set; }
        public string Unit { get; set; }
        public string Quantity { get; set; }
        public string Currency { get; set; }
        public Connector? Connector { get; set; }
    }
}