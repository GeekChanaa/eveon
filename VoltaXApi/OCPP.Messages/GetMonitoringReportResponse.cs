using System;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class GetMonitoringReportResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public GenericDeviceModelStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

}