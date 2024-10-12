using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class SignCertificateRequest
  {
      public CustomDataType? customData { get; set; }

      [Required]
      [MaxLength(5500)]
      public string csr { get; set; }

      public CertificateSigningUseEnumType? certificateType { get; set; }
  }

  

}