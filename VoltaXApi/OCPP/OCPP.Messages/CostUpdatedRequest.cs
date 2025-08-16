namespace VoltaXApi.OCPP.Messages
{
  public class CostUpdatedRequest
  {
      public CustomDataType CustomData { get; set; } 

      public double TotalCost { get; set; } 

      public string TransactionId { get; set; } 
  }

}