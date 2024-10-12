namespace VoltaXApi.OCPP.Messages
{
  public class ClearCacheResponse
  {
      public CustomDataType CustomData { get; set; }
      public ClearCacheStatusEnum Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum ClearCacheStatusEnum
  {
      Accepted,
      Rejected
  }

}