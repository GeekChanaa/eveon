namespace VoltaXApi.OCPP.Messages
{
  public class BootNotificationRequest
  {
      public CustomDataType CustomData { get; set; }
      public ChargingStationType ChargingStation { get; set; }
      public BootReasonEnumType Reason { get; set; }
  }

  public enum BootReasonEnumType
  {
      ApplicationReset,
      FirmwareUpdate,
      LocalReset,
      PowerUp,
      RemoteReset,
      ScheduledReset,
      Triggered,
      Unknown,
      Watchdog
  }

  public class ChargingStationType
  {
      public CustomDataType CustomData { get; set; }
      public string SerialNumber { get; set; }
      public string Model { get; set; }
      public ModemType Modem { get; set; }
      public string VendorName { get; set; }
      public string FirmwareVersion { get; set; }
  }

  public class ModemType
  {
      public CustomDataType CustomData { get; set; }
      public string Iccid { get; set; }
      public string Imsi { get; set; }
  }

}