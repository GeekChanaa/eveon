using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetNetworkProfileResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public SetNetworkProfileStatusEnumType Status { get; set; }
      
      public StatusInfoType StatusInfo { get; set; }
  }

  public enum SetNetworkProfileStatusEnumType
  {
      Accepted,
      Rejected,
      Failed
  }
}