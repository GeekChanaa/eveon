using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class GetTransactionStatusResponse
  {
      public CustomDataType CustomData { get; set; }

      public bool? OngoingIndicator { get; set; }

      [Required]
      public bool MessagesInQueue { get; set; }
  }

}