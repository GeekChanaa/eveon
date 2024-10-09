using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class RequestStopTransactionRequest
  {
      [Required]
      [MaxLength(36)]
      public string TransactionId { get; set; }

      public CustomDataType CustomData { get; set; }
  }


}