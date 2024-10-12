using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetInstalledCertificateIdsRequest
  {
      public CustomDataType CustomData { get; set; }

      [MinLength(1)]
      public List<GetCertificateIdUseEnumType> CertificateType { get; set; }
  }

}