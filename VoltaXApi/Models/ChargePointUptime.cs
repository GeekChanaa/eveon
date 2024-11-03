

namespace VoltaXApi.Models
{
  public class ChargePointUptime: IEntity
  {
    public int ID { get; set; }
    public int ChargePointID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ChargePointUptimeStatusEnum ChargePointUptimeStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
  }
}