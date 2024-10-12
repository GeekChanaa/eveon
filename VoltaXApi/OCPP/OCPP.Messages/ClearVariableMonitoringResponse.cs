namespace VoltaXApi.OCPP.Messages
{
  public class ClearVariableMonitoringResponse
  {
      public CustomDataType CustomData { get; set; } 

      public List<ClearMonitoringResultType> ClearMonitoringResult { get; set; } 
  }

  public class ClearMonitoringResultType
  {
      public CustomDataType CustomData { get; set; } 

      public ClearMonitoringStatusEnumType Status { get; set; } 

      public int Id { get; set; } 

      public StatusInfoType StatusInfo { get; set; } 
  }

  public enum ClearMonitoringStatusEnumType
  {
      Accepted,
      Rejected,
      NotFound
  }

}