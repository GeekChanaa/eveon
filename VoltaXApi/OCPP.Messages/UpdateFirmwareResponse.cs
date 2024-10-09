using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class UpdateFirmwareResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public UpdateFirmwareStatusEnumType Status { get; set; }

      public StatusInfoType? StatusInfo { get; set; }
  }


  public enum UpdateFirmwareStatusEnumType
  {
      Accepted,
      Rejected,
      AcceptedCanceled,
      InvalidCertificate,
      RevokedCertificate
  }

}