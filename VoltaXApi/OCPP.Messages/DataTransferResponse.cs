namespace VoltaXApi.OCPP.Messages
{
  public class DataTransferResponse
  {
      public CustomDataType CustomData { get; set; }
      public DataTransferStatusEnumType Status { get; set; } // Required
      public StatusInfoType StatusInfo { get; set; }
      public object Data { get; set; }
  }

  public enum DataTransferStatusEnumType
  {
      Accepted,
      Rejected,
      UnknownMessageId,
      UnknownVendorId
  }

}