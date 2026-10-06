import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfirmService } from 'src/_services/confirm.service';
import { AccessService } from 'src/_services/access.service';
import {
  ChargePointCertificates, ChargerCertificate, ChargerPkiService, InstalledCertificate, RootCertificateType
} from 'src/_services/charger-pki.service';

@Component({
  selector: 'app-charge-point-certificates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './charge-point-certificates.component.html',
  styleUrls: ['./charge-point-certificates.component.sass']
})
export class ChargePointCertificatesComponent implements OnChanges {
  @Input() chargePointId = '';

  data: ChargePointCertificates | null = null;
  loading = false;
  busy = '';
  error = '';
  message = '';
  hints: string[] = [];

  rootType: RootCertificateType = 'CSMSRootCertificate';
  rootSource: 'ca' | 'pem' = 'pem';
  rootPem = '';
  showRootForm = false;

  profile = 1;
  force = false;

  constructor(private pki: ChargerPkiService, private confirm: ConfirmService, public access: AccessService) {}

  get isAdmin(): boolean { return this.access.isAdmin; }

  ngOnChanges(): void {
    if (this.chargePointId) this.load();
  }

  load(): void {
    this.loading = true;
    this.pki.certificates(this.chargePointId).subscribe({
      next: data => {
        this.data = data;
        this.profile = data.securityProfile;
        this.loading = false;
      },
      error: err => { this.loading = false; this.error = this.text(err, 'Could not load the certificates.'); }
    });
  }

  daysLeft(certificate: ChargerCertificate): number {
    return Math.floor((new Date(certificate.notAfter).getTime() - Date.now()) / 86400000);
  }

  refreshInstalled(): void {
    this.run('refresh', this.pki.refreshInstalled(this.chargePointId), 'Installed certificates');
  }

  triggerRenewal(): void {
    this.confirm.requestConfirmation('Request a new certificate',
      'The charger is asked to send a certificate signing request (TriggerMessage SignChargingStationCertificate). The CSMS signs it with the charger CA and sends it back; the current certificate is replaced once the charger accepts the new one.',
      () => this.run('renew', this.pki.triggerRenewal(this.chargePointId), 'Renewal request'),
      { confirmLabel: 'Request certificate' });
  }

  installRoot(): void {
    if (this.rootSource === 'pem' && !this.rootPem.trim()) { this.error = 'Paste the PEM certificate to install.'; return; }
    const label = this.rootType === 'CSMSRootCertificate' ? 'CSMS root' : 'manufacturer root';
    this.confirm.requestConfirmation(`Install ${label} certificate`,
      this.rootType === 'CSMSRootCertificate'
        ? 'The charger uses CSMS root certificates to verify the server certificate of the CSMS. Install the root of the certificate the chargers see (e.g. ISRG Root X1 behind Caddy / Let\'s Encrypt); the charger CA only when the CSMS server certificate is issued by it.'
        : 'The charger uses manufacturer root certificates to verify signed firmware.',
      () => this.run('root', this.pki.installRoot(this.chargePointId, this.rootType, this.rootSource === 'ca', this.rootSource === 'pem' ? this.rootPem : undefined),
        'InstallCertificate', () => { this.showRootForm = false; this.rootPem = ''; }),
      { confirmLabel: 'Install' });
  }

  deleteInstalled(certificate: InstalledCertificate): void {
    this.confirm.requestConfirmation('Delete installed certificate',
      `Delete the ${certificate.certificateType} with serial ${certificate.serialNumber} from the charger?` +
      (certificate.isChargerCa ? ' This is the charger CA: removing it can break the charger\'s TLS connection.' : ''),
      () => this.run('delete-' + certificate.id, this.pki.deleteInstalled(this.chargePointId, certificate), 'DeleteCertificate'),
      { confirmLabel: 'Delete', tone: 'danger' });
  }

  revoke(certificate: ChargerCertificate): void {
    this.confirm.requestConfirmation('Revoke certificate',
      `Certificate ${certificate.serialNumber} will no longer be accepted for security profile 3 connections. If it is the charger's current certificate, the charger is refused at its next connection until a new certificate is issued.`,
      () => {
        this.busy = 'revoke-' + certificate.id;
        this.pki.revoke(this.chargePointId, certificate.id, 'Revoked from the dashboard').subscribe({
          next: () => { this.busy = ''; this.message = `Certificate ${certificate.serialNumber} revoked.`; this.load(); },
          error: err => { this.busy = ''; this.error = this.text(err, 'Could not revoke the certificate.'); }
        });
      },
      { confirmLabel: 'Revoke', tone: 'danger' });
  }

  setProfile(): void {
    if (!this.data) return;
    const profile = Number(this.profile);
    const warnings: Record<number, string> = {
      1: 'Profile 1 accepts Basic authentication without TLS when the server allows it (development only). This lowers the charger\'s security.',
      2: 'Profile 2 requires wss:// (TLS) and Basic authentication. The charger must trust the CSMS server certificate root.',
      3: 'Profile 3 requires wss:// and a client certificate issued by the charger CA; Basic authentication is no longer accepted. A charger without a valid certificate will be refused.'
    };
    const forceText = this.force
      ? ' The CSMS-side profile is changed even if the charger does not accept it: a charger that has not switched yet will be disconnected and refused.'
      : ' The CSMS-side profile only changes if the charger accepts SecurityCtrlr.SecurityProfile.';
    this.confirm.requestConfirmation(`Set security profile ${profile}`,
      warnings[profile] + forceText,
      () => {
        this.busy = 'profile';
        this.error = this.message = '';
        this.hints = [];
        this.pki.setSecurityProfile(this.chargePointId, profile, this.force).subscribe({
          next: result => {
            this.busy = '';
            this.force = false;
            this.message = `Charger answered ${result.chargerStatus}${result.chargerStatusInfo ? ' (' + result.chargerStatusInfo + ')' : ''}. ` +
              (result.csmsUpdated ? `The CSMS now enforces profile ${result.csmsProfile}.` : `The CSMS still enforces profile ${result.csmsProfile}.`);
            this.hints = result.hints;
            this.load();
          },
          error: err => { this.busy = ''; this.error = this.text(err, 'Could not change the security profile.'); }
        });
      },
      { confirmLabel: 'Change profile', tone: 'danger' });
  }

  private run(key: string, request: Observable<{ message: string; status?: string }>, action: string, done?: () => void): void {
    this.busy = key;
    this.error = this.message = '';
    request.subscribe({
      next: answer => { this.busy = ''; this.message = answer.message || `${action}: done.`; done?.(); this.load(); },
      error: err => { this.busy = ''; this.error = this.text(err, `${action} failed.`); }
    });
  }

  private text(err: HttpErrorResponse, fallback: string): string {
    if (err.status === 403) return 'Only administrators can do this.';
    return err.error?.message || err.error?.error || fallback;
  }
}
