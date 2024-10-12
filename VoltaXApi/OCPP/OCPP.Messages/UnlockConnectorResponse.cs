using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class UnlockConnectorResponse
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public UnlockStatusEnumType Status { get; set; }

      public StatusInfoType? StatusInfo { get; set; }
  }

  public enum UnlockStatusEnumType
  {
      Unlocked,
      UnlockFailed,
      OngoingAuthorizedTransaction,
      UnknownConnector
  }

}