namespace VoltaXApi.OCPP.Messages
{
  public class CostUpdatedRequest
  {
      public CustomDataType CustomData { get; set; } 

      public decimal TotalCost { get; set; } 

      public string TransactionId { get; set; } 
  }

}