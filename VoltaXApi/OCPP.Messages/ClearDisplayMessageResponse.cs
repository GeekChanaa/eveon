namespace VoltaXApi.OCPP.Messages
{
  public class ClearDisplayMessageResponse
  {
      public CustomDataType CustomData { get; set; }
      public ClearMessageStatusEnum Status { get; set; } 
      public StatusInfoType StatusInfo { get; set; } 
  }


  public enum ClearMessageStatusEnum
  {
      Accepted, 
      Unknown   
  }


}