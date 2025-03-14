namespace VoltaXApi.Models;
public class OCPPConfigurationComponent: IEntity
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string? Instance { get; set; }    
    public int? OCPPConfigurationEVSEID { get; set; }
    public OCPPConfigurationEVSE? OCPPConfigurationEVSE { get; set; }
    public ICollection<OCPPConfigurationItem>? ConfigurationItems { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}