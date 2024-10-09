using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class GetBaseReportRequest
  {
      [Required]
      public int RequestId { get; set; }

      [Required]
      public ReportBaseEnumType ReportBase { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public enum ReportBaseEnumType
  {
      ConfigurationInventory,
      FullInventory,
      SummaryInventory
  }

}