using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class OCSPRequestDataType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public HashAlgorithmEnumType HashAlgorithm { get; set; }

      [Required]
      [MaxLength(128)]
      public string IssuerNameHash { get; set; }

      [Required]
      [MaxLength(128)]
      public string IssuerKeyHash { get; set; }

      [Required]
      [MaxLength(40)]
      public string SerialNumber { get; set; }

      [Required]
      [MaxLength(512)]
      public string ResponderURL { get; set; }
  }
}