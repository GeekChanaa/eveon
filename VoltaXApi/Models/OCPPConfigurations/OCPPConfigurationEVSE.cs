namespace VoltaXApi.Models;
public class OCPPConfigurationEVSE: IEntity
{
    public int ID { get; set; }
    public int EVSEId { get; set; }
    public int? ConnectorId { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}