using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class Get15118EVCertificateRequest
  {
      [Required]
      [MaxLength(50)]
      public string Iso15118SchemaVersion { get; set; }

      [Required]
      public CertificateActionEnumType Action { get; set; }

      [Required]
      [MaxLength(5600)]
      public string ExiRequest { get; set; }

      public CustomDataType CustomData { get; set; }
  }

  public enum CertificateActionEnumType
  {
      Install,
      Update
  }

}