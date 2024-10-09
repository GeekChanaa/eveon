using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class RequestStopTransactionResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public RequestStartStopStatusEnum Status { get; set; }

      public StatusInfo StatusInfo { get; set; }
  }
  public enum RequestStartStopStatusEnum
  {
      Accepted,
      Rejected
  }

  public class StatusInfo
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      [MaxLength(20)]
      public string ReasonCode { get; set; }

      [MaxLength(512)]
      public string AdditionalInfo { get; set; }
  }

}