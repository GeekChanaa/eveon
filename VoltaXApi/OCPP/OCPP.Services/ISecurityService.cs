using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Pki;

namespace VoltaXApi.OCPP.Core
{
  public interface ISecurityService
  {
    Task<InstallCertificateResponse> InstallCertificate(string chargePointID, InstallCertificateRequest request, CancellationToken cancellationToken = default);
    Task<InstallCertificateResponse> InstallRootCertificate(string chargePointID, InstallRootCertificateDto request, CancellationToken cancellationToken = default);
    Task<DeleteCertificateResponse> DeleteCertificate(string chargePointID, CertificateHashDataType hashData, CancellationToken cancellationToken = default);
    Task<GetInstalledCertificateIdsResponse> GetInstalledCertificateIds(string chargePointID, List<GetCertificateIdUseEnumType>? certificateTypes, CancellationToken cancellationToken = default);
    Task<TriggerMessageResponse> TriggerCertificateRenewal(string chargePointID, CancellationToken cancellationToken = default);
    Task<SecurityProfileChangeResult> SetSecurityProfile(string chargePointID, SetSecurityProfileDto request, CancellationToken cancellationToken = default);
    Task<ChargePointCertificatesDto?> GetCertificates(string chargePointID, CancellationToken cancellationToken = default);
    Task<ChargerCertificateDto?> RevokeCertificate(string chargePointID, int certificateID, string? reason, CancellationToken cancellationToken = default);
  }
}
