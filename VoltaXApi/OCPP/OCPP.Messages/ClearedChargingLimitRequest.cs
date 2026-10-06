namespace VoltaXApi.OCPP.Messages
{
  public class ClearedChargingLimitRequest
  {
      public CustomDataType CustomData { get; set; } 
      public ChargingLimitSourceEnum ChargingLimitSource { get; set; } 
      public int? EvseId { get; set; }
  }

  public enum ChargingLimitSourceEnum
  {
      EMS,   
      Other, 
      SO,    
      CSO    
  }

}