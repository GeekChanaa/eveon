namespace VoltaXApi.OCPP.Messages
{
  public class DataTransferRequest
  {
      public CustomDataType CustomData { get; set; }
      public string MessageId { get; set; }
      public object Data { get; set; }
      public string VendorId { get; set; } // Required
  }

}