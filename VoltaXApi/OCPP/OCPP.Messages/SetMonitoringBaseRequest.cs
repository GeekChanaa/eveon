using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetMonitoringBaseRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public MonitoringBaseEnumType MonitoringBase { get; set; }
  }

  public enum MonitoringBaseEnumType
  {
      All,
      FactoryDefault,
      HardWiredOnly
  }

}