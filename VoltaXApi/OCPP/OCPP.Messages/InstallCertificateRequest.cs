using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{

  public class InstallCertificateRequest
  {
      [Required]
      public InstallCertificateUseEnumType CertificateType { get; set; }

      [Required]
      [MaxLength(5500)]
      public string Certificate { get; set; }

      public CustomDataType? CustomData { get; set; }
  }

  public enum InstallCertificateUseEnumType
  {
      V2GRootCertificate,
      MORootCertificate,
      CSMSRootCertificate,
      ManufacturerRootCertificate
  }

}