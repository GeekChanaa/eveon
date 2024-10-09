using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class UnlockConnectorRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public int EvseId { get; set; }

      [Required]
      public int ConnectorId { get; set; }
  }

}