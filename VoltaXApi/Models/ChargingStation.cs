namespace VoltaXApi.Models
{
    public class ChargingStation
    {
        public int ID { get; set; }
        public string Name { get; set; } // name of the station
        public string BusinessHours { get; set; } // business hours of the station
        public string Address { get; set; } // location of the station

        // Navigation properties
        public ICollection<ChargePoint> ChargePoints { get; set; }
    }
}