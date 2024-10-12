using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SendLocalListResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public SendLocalListStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum SendLocalListStatusEnumType
  {
      Accepted,
      Failed,
      VersionMismatch
  }

}