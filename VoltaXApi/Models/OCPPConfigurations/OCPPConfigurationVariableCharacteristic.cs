using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;
public class OCPPConfigurationVariableCharacteristic: IEntity
{
    public int ID { get; set; }
    public string? Unit { get; set; }
    public DataEnumType DataType { get; set; }
    public double? MinLimit { get; set; }
    public double? MaxLimit { get; set; }
    public string? ValuesList { get; set; }
    public bool SupportsMonitoring { get; set; }    
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}