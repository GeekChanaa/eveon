using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.OCPP.Messages
{
  public class GetBaseReportResponse
  {
      [Required]
      public GenericDeviceModelStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }
}