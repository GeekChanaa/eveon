using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class NotifyCustomerInformationResponse
  {
      public CustomDataType CustomData { get; set; }
  }
}
