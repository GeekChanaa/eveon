namespace VoltaXApi.Models;

public class ElectricVehicleModel : IEntity
{
    public int ID { get; set; }
    public string Model { get; set; }
    public string Make { get; set; }
    public string Type { get; set; }
    public string Range { get; set; }
    public string BatteryCapacity { get; set; }
    public string ImageSrc { get; set; }
    public string LogoSrc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
