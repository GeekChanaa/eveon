using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class LogStatusNotificationRequest
  {
      [Required]
      public UploadLogStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public int? RequestId { get; set; } // Nullable, as it's not required in all cases
  }

  public enum UploadLogStatusEnumType
  {
      BadMessage,
      Idle,
      NotSupportedOperation,
      PermissionDenied,
      Uploaded,
      UploadFailure,
      Uploading,
      AcceptedCanceled
  }

}