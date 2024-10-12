using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class InstallCertificateResponse
  {
      [Required]
      public InstallCertificateStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum InstallCertificateStatusEnumType
  {
      Accepted,
      Rejected,
      Failed
  }

}