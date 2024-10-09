namespace VoltaXApi.OCPP.Messages
{
  public class CertificateSignedRequest
  {
      public CustomDataType CustomData { get; set; }
      public string CertificateChain { get; set; } // The signed PEM encoded X.509 certificate
      public CertificateSigningUseEnumType? CertificateType { get; set; } // Nullable since it's not required
  }

  public enum CertificateSigningUseEnumType
  {
      ChargingStationCertificate,
      V2GCertificate
  }

}