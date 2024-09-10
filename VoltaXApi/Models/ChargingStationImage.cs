namespace VoltaXApi.Models
{
    public class ChargingStationImage  : IEntity
    {
    
        public int ID { get; set; }
        public int ChargingStationID { get; set; }
        public int ImageID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ChargingStation? ChargingStation { get; set; }
        public Image? Image { get; set; }
    }
}