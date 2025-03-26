using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ChangeAvailabilityRequest
  {
      public CustomDataType? CustomData { get; set; }
      public EVSEType Evse { get; set; }
      [Required]
      public OperationalStatusEnumType OperationalStatus { get; set; }
  }

  public enum OperationalStatusEnumType
  {
      Inoperative,
      Operative
  }

}