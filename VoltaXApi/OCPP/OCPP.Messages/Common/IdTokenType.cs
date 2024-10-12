namespace VoltaXApi.OCPP.Messages
{
  public class IdTokenType
  {
      public CustomDataType CustomData { get; set; }
      public List<AdditionalInfoType> AdditionalInfo { get; set; }
      public string IdToken { get; set; }
      public IdTokenEnumType Type { get; set; }
  }
}