

namespace VoltaXApi.OCPP.Messages
{
  public class CostType
  {
    public CustomDataType customData { get; set; }

    public CostKindEnumType costKind { get; set; }

    public double amount { get; set; }
  }

}