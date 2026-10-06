import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

export interface ChargerCertificate {
  id: number;
  certificateType: string;
  serialNumber: string;
  subject: string;
  thumbprintSha256: string;
  notBefore: string;
  notAfter: string;
  status: 'Pending' | 'Active' | 'Replaced' | 'Revoked' | 'Rejected' | 'Failed';
  statusReason?: string;
  issuedAt: string;
  statusChangedAt?: string;
}

export interface InstalledCertificate {
  id: number;
  certificateType: string;
  hashAlgorithm: string;
  issuerNameHash: string;
  issuerKeyHash: string;
  serialNumber: string;
  reportedAt: string;
  isChargerCa: boolean;
}

export interface ChargePointCertificates {
  chargePointId: string;
  securityProfile: number;
  hasPassword: boolean;
  hasPinnedCertificate: boolean;
  connected: boolean;
  protocol?: string;
  caConfigured: boolean;
  current?: ChargerCertificate;
  certificates: ChargerCertificate[];
  installed: InstalledCertificate[];
  installedReportedAt?: string;
}

export interface OcppCommandAnswer { message: string; status?: string; response?: any; }

export interface SecurityProfileChange {
  requestedProfile: number;
  csmsProfile: number;
  csmsUpdated: boolean;
  chargerStatus: string;
  chargerStatusInfo?: string;
  hints: string[];
}

export interface ChargerCaInfo {
  configured: boolean;
  source: string;
  subject?: string;
  serialNumber?: string;
  thumbprintSha256?: string;
  notBefore?: string;
  notAfter?: string;
  keyAlgorithm?: string;
  error?: string;
}

export type RootCertificateType = 'CSMSRootCertificate' | 'ManufacturerRootCertificate';

@Injectable({ providedIn: 'root' })
export class ChargerPkiService {
  private readonly baseUrl = environment.apiUrl + '/ocpp/Security';

  constructor(private http: HttpClient) {}

  private id(chargePointId: string) { return encodeURIComponent(chargePointId); }

  certificates(chargePointId: string) {
    return this.http.get<ChargePointCertificates>(`${this.baseUrl}/Certificates/${this.id(chargePointId)}`);
  }

  refreshInstalled(chargePointId: string) {
    return this.http.post<OcppCommandAnswer>(`${this.baseUrl}/GetInstalledCertificateIds/${this.id(chargePointId)}`, {});
  }

  triggerRenewal(chargePointId: string) {
    return this.http.post<OcppCommandAnswer>(`${this.baseUrl}/TriggerCertificateRenewal/${this.id(chargePointId)}`, {});
  }

  installRoot(chargePointId: string, certificateType: RootCertificateType, useChargerCa: boolean, certificatePem?: string) {
    return this.http.post<OcppCommandAnswer>(`${this.baseUrl}/InstallRootCertificate/${this.id(chargePointId)}`, { certificateType, useChargerCa, certificatePem });
  }

  deleteInstalled(chargePointId: string, certificate: InstalledCertificate) {
    return this.http.post<OcppCommandAnswer>(`${this.baseUrl}/DeleteCertificate/${this.id(chargePointId)}`, {
      hashAlgorithm: certificate.hashAlgorithm,
      issuerNameHash: certificate.issuerNameHash,
      issuerKeyHash: certificate.issuerKeyHash,
      serialNumber: certificate.serialNumber
    });
  }

  setSecurityProfile(chargePointId: string, securityProfile: number, force: boolean) {
    return this.http.post<SecurityProfileChange>(`${this.baseUrl}/SetSecurityProfile/${this.id(chargePointId)}`, { securityProfile, force });
  }

  revoke(chargePointId: string, certificateId: number, reason: string) {
    return this.http.post<ChargerCertificate>(`${this.baseUrl}/RevokeCertificate/${this.id(chargePointId)}/${certificateId}`, { reason });
  }

  ca() { return this.http.get<ChargerCaInfo>(`${this.baseUrl}/Ca`); }

  caCertificate() { return this.http.get(`${this.baseUrl}/Ca/Certificate`, { responseType: 'blob' }); }
}
