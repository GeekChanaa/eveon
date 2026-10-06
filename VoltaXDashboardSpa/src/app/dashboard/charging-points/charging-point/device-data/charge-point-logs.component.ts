import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChargerDeviceDataService, LogUploadTicket, UploadedChargerLog } from 'src/_services/charger-device-data.service';

/** GetLog requests of a charger (one-time upload URLs) and the files it uploaded. Admin only. */
@Component({
  selector: 'app-charge-point-logs',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['./device-data.sass'],
  template: `
    <div class="section-intro"><div><h2>Charger logs</h2><p>Ask the charger to upload a log. It receives a one-time upload address valid for two hours; the file then appears here.</p></div>
      <div class="device-actions">
        <button type="button" class="secondary-action" [disabled]="busy" (click)="request('SecurityLog')">Request security log</button>
        <button type="button" class="primary-action" [disabled]="busy" (click)="request('DiagnosticsLog')">{{ busy ? 'Requesting...' : 'Request diagnostics log' }}</button>
      </div></div>
    <p class="device-feedback" role="status" *ngIf="feedback">{{ feedback }}</p>
    <p class="field-error" role="alert" *ngIf="error">{{ error }}</p>
    <div class="device-table">
      <table>
        <thead><tr><th>Requested (UTC)</th><th>Type</th><th>Status</th><th>Upload address</th><th>Files</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="5" class="device-empty">Loading...</td></tr>
          <tr *ngIf="!loading && tickets.length === 0"><td colspan="5" class="device-empty">No log requested yet.</td></tr>
          <ng-container *ngIf="!loading">
            <tr *ngFor="let t of tickets">
              <td>{{ t.createdAt | date:'yyyy-MM-dd HH:mm':'UTC' }}<small> request {{ t.requestId }}</small></td>
              <td>{{ t.purpose }}</td>
              <td>{{ t.status || 'Sent' }}<small *ngIf="t.statusAt"> {{ t.statusAt | date:'HH:mm:ss':'UTC' }}</small></td>
              <td><small>{{ t.usedAt ? 'Used ' + (t.usedAt | date:'yyyy-MM-dd HH:mm':'UTC') : isExpired(t) ? 'Expired' : 'Valid until ' + (t.expiresAt | date:'yyyy-MM-dd HH:mm':'UTC') }}</small></td>
              <td>
                <span *ngIf="!t.files.length" class="device-muted">-</span>
                <div *ngFor="let f of t.files">
                  <button type="button" class="edit-button" [disabled]="downloading === f.id" (click)="download(f)">{{ f.fileName }}</button>
                  <small> {{ size(f.sizeBytes) }} &middot; sha256 {{ f.sha256.slice(0, 12) }}&hellip;</small>
                </div>
              </td>
            </tr>
          </ng-container>
        </tbody>
      </table>
    </div>`
})
export class ChargePointLogsComponent implements OnInit {
  @Input() chargePointId = '';
  tickets: LogUploadTicket[] = [];
  loading = false;
  busy = false;
  downloading: number | null = null;
  error = '';
  feedback = '';

  constructor(private data: ChargerDeviceDataService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.data.logs(this.chargePointId).subscribe({
      next: tickets => { this.tickets = tickets; this.loading = false; },
      error: () => { this.loading = false; this.error = 'Could not load the charger logs.'; }
    });
  }

  isExpired(t: LogUploadTicket): boolean { return new Date(t.expiresAt).getTime() < Date.now(); }

  size(bytes: number): string {
    return bytes < 1024 ? `${bytes} B` : bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(1)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  request(logType: 'DiagnosticsLog' | 'SecurityLog'): void {
    this.busy = true;
    this.error = '';
    this.feedback = '';
    this.data.requestLog(this.chargePointId, logType).subscribe({
      next: result => {
        this.busy = false;
        if (result?.status === 'Accepted' || result?.status === 'AcceptedCanceled') this.feedback = 'The charger accepted the request; the upload status updates below.';
        else this.error = result?.message || 'The charger refused the request.';
        this.load();
      },
      error: err => { this.busy = false; this.error = err?.error?.message || 'The request failed.'; this.load(); }
    });
  }

  download(file: UploadedChargerLog): void {
    this.downloading = file.id;
    this.data.downloadLog(file.id).subscribe({
      next: blob => {
        this.downloading = null;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = file.fileName;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => { this.downloading = null; this.error = 'Could not download the file.'; }
    });
  }
}
