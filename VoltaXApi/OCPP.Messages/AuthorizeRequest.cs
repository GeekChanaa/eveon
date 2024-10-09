using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace VoltaXApi.OCPP.Messages
{
  public class AuthorizeRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public IdTokenType IdToken { get; set; }

      [MaxLength(5500)]
      public string Certificate { get; set; }

      [MaxLength(4)]
      public List<OCSPRequestDataType> Iso15118CertificateHashData { get; set; }
  }

}