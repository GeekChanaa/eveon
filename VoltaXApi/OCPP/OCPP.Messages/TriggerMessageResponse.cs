using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class TriggerMessageResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public TriggerMessageStatusEnumType Status { get; set; }

      public StatusInfoType? StatusInfo { get; set; }
  }

  public enum TriggerMessageStatusEnumType
  {
      Accepted,
      Rejected,
      NotImplemented
  }
}