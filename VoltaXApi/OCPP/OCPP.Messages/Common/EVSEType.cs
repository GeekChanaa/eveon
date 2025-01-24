

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class EVSEType
  {
      [Required]
      public int Id { get; set; }

      public int? ConnectorId { get; set; }
      public CustomDataType? CustomData { get; set; }
  }
}