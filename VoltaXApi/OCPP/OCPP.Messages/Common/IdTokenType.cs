using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class IdTokenType
  {
      public CustomDataType? CustomData { get; set; }
      public List<AdditionalInfoType>? AdditionalInfo { get; set; }
      [Required]
      public string IdToken { get; set; }
      [Required]
      public IdTokenEnumType Type { get; set; }
  }
}