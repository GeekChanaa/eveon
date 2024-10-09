namespace VoltaXApi.OCPP.Messages
{
  public class CustomerInformationResponse
  {
      public CustomDataType CustomData { get; set; }
      public CustomerInformationStatusEnumType Status { get; set; }
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum CustomerInformationStatusEnumType
  {
      Accepted,
      Rejected,
      Invalid
  }

}