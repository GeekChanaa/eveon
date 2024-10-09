using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetInstalledCertificateIdsResponse
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public GetInstalledCertificateStatusEnumType Status { get; set; }

      public StatusInfoType StatusInfo { get; set; }

      [MinLength(1)]
      public List<CertificateHashDataChainType> CertificateHashDataChain { get; set; }
  }

  public enum GetInstalledCertificateStatusEnumType
  {
      Accepted,
      NotFound
  }

  public class CertificateHashDataChainType
  {
      [Required]
      public GetCertificateIdUseEnumType CertificateType { get; set; }

      [Required]
      public CertificateHashDataType CertificateHashData { get; set; }

      public CustomDataType CustomData { get; set; }

      [MinLength(1)]
      [MaxLength(4)]
      public List<CertificateHashDataType> ChildCertificateHashData { get; set; }
  }

}