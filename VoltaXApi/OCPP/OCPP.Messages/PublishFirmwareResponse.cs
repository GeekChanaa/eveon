using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class PublishFirmwareResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public GenericStatusEnum Status { get; set; }

      public StatusInfoType? StatusInfo { get; set; }
  }

  public enum GenericStatusEnum
  {
      Accepted,
      Rejected
  }
}