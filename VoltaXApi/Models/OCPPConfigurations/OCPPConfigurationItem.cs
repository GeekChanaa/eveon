namespace VoltaXApi.Models;
public class OCPPConfigurationItem: IEntity
{
    public int ID { get; set; }
    public int ChargePointID { get; set; }
    public int OCPPConfigurationComponentID { get; set; }
    public int OCPPConfigurationVariableID { get; set; }
    public OCPPConfigurationComponent? OCPPConfigurationComponent { get; set; }
    public OCPPConfigurationVariable? OCPPConfigurationVariable { get; set; }
    public ICollection<OCPPConfigurationVariableAttribute>? OCPPConfigurationVariableAttributes { get; set; }
    public int? OCPPConfigurationVariableCharacteristicID { get; set; }
    public OCPPConfigurationVariableCharacteristic? OCPPConfigurationVariableCharacteristic { get; set; }
    public ChargePoint? ChargePoint { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}