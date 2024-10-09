using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class TriggerMessageRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public EVSEType Evse { get; set; }

      [Required]
      public MessageTriggerEnumType RequestedMessage { get; set; }
  }

  public enum MessageTriggerEnumType
  {
      BootNotification,
      LogStatusNotification,
      FirmwareStatusNotification,
      Heartbeat,
      MeterValues,
      SignChargingStationCertificate,
      SignV2GCertificate,
      StatusNotification,
      TransactionEvent,
      SignCombinedCertificate,
      PublishFirmwareStatusNotification
  }

}