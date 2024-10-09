using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetLocalListVersionResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public int VersionNumber { get; set; }
  }
}