using VoltaXApi.Models;
namespace VoltaXApi.Dtos
{
    public class ChargingStationRevenue
    {
        public int ChargingStationID { get; set; }
        public string ChargingStationName { get; set; }
        public decimal Revenue { get; set; }
    }
}

