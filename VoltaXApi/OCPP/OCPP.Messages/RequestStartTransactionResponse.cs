using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class RequestStartTransactionResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public RequestStartStopStatusEnum Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      /// <summary>Set when the charger already started a transaction (e.g. authorized locally before the request arrived).</summary>
      public string? TransactionId { get; set; }
  }
}
