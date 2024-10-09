using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class MeterValuesResponse
  {
      public CustomDataType? CustomData { get; set; }
  }

}