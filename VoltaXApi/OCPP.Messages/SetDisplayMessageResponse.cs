using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class SetDisplayMessageResponse
  {
      public CustomDataType? CustomData { get; set; }
      [Required]
      public DisplayMessageStatusEnumType Status { get; set; }
      public StatusInfoType? StatusInfo { get; set; }
  }

  public enum DisplayMessageStatusEnumType
  {
      Accepted,
      NotSupportedMessageFormat,
      Rejected,
      NotSupportedPriority,
      NotSupportedState,
      UnknownTransaction
  }
}