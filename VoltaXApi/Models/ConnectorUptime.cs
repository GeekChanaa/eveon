

namespace VoltaXApi.Models
{
  public class ConnectorUptime: IEntity
  {
    public int ID { get; set; }
    public int ConnectorID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TransactionID { get; set; }
    public ConnectorUptimeStatusEnum ConnectorUptimeStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Transaction? Transaction { get; set; }
    
  }
}