namespace VoltaXApi.OCPP.Messages
{

  public class ClearVariableMonitoringRequest
  {
      public CustomDataType? CustomData { get; set; } 

      public List<int> Id { get; set; } 
  }

}