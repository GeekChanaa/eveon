import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChargerDeviceDataService, CustomerInformationReport, newRequestId } from 'src/_services/charger-device-data.service';
import { AccessService } from 'src/_services/access.service';

/** CustomerInformation requests sent to the charger and the customer data it returned (NotifyCustomerInformation). */
@Component({
  selector: 'app-charge-point-customer-information',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['./device-data.sass'],
  template: `
    <div class="section-intro"><div><h2>Customer information</h2><p>Data the charger holds about a customer (identified by token or customer identifier), as returned by the charger.</p></div>
      <button type="button" class="secondary-action" [disabled]="loading" (click)="load()">Reload</button></div>
    <form class="device-form" *ngIf="canOperate" (ngSubmit)="request()">
      <h3>Request customer information</h3>
      <label><span>Id token</span><input name="idToken" [(ngModel)]="draft.idToken" maxlength="36" placeholder="Card number"></label>
      <label><span>Customer identifier</span><input name="customerIdentifier" [(ngModel)]="draft.customerIdentifier" maxlength="64"></label>
      <label class="device-check"><input type="checkbox" name="clear" [(ngModel)]="draft.clear"><span>Also clear this data on the charger</span></label>
      <div class="edit-actions"><button type="submit" class="primary-action" [disabled]="busy || (!draft.idToken.trim() && !draft.customerIdentifier.trim())">{{ busy ? 'Sending...' : 'Send request' }}</button></div>
    </form>
    <p class="device-feedback" role="status" *ngIf="feedback">{{ feedback }}</p>
    <p class="field-error" role="alert" *ngIf="error">{{ error }}</p>
    <div class="device-table">
      <table>
        <thead><tr><th>Requested (UTC)</th><th>Customer</th><th>Status</th><th>Data</th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="4" class="device-empty">Loading...</td></tr>
          <tr *ngIf="!loading && items.length === 0"><td colspan="4" class="device-empty">No customer information requested yet.</td></tr>
          <ng-container *ngIf="!loading">
            <tr *ngFor="let r of items">
              <td>{{ r.requestedAt | date:'yyyy-MM-dd HH:mm':'UTC' }}<small> request {{ r.requestId }}</small></td>
              <td>{{ r.idToken || r.customerIdentifier || '-' }}<small *ngIf="r.clear"> (clear)</small></td>
              <td>{{ r.commandStatus || 'Pending' }}<small> &middot; {{ r.complete ? 'complete' : r.partsReceived + ' part(s)' }}</small></td>
              <td><pre class="device-data">{{ r.data || '-' }}</pre></td>
            </tr>
          </ng-container>
        </tbody>
      </table>
    </div>
    <div class="device-pager" *ngIf="totalCount > pageSize">
      <button type="button" class="button-stroke" [disabled]="page <= 1" (click)="goTo(page - 1)">Previous</button>
      <span>Page {{ page }}</span>
      <button type="button" class="button-stroke" [disabled]="page * pageSize >= totalCount" (click)="goTo(page + 1)">Next</button>
    </div>`
})
export class ChargePointCustomerInformationComponent implements OnInit {
  @Input() chargePointId = '';
  readonly pageSize = 10;
  items: CustomerInformationReport[] = [];
  page = 1;
  totalCount = 0;
  loading = false;
  busy = false;
  error = '';
  feedback = '';
  draft = { idToken: '', customerIdentifier: '', clear: false };

  constructor(private data: ChargerDeviceDataService, private access: AccessService) {}

  get canOperate(): boolean { return this.access.can('OperateChargePoints'); }

  ngOnInit(): void { this.load(); }

  goTo(page: number): void { this.page = page; this.load(); }

  load(): void {
    this.loading = true;
    this.data.customerInformation(this.chargePointId, this.page, this.pageSize).subscribe({
      next: result => { this.items = result.items; this.totalCount = result.totalCount; this.loading = false; },
      error: () => { this.loading = false; this.error = 'Could not load the customer information.'; }
    });
  }

  request(): void {
    const request: any = { requestId: newRequestId(), report: true, clear: this.draft.clear };
    if (this.draft.idToken.trim()) request.idToken = { idToken: this.draft.idToken.trim(), type: 'ISO14443' };
    if (this.draft.customerIdentifier.trim()) request.customerIdentifier = this.draft.customerIdentifier.trim();
    this.busy = true;
    this.error = '';
    this.feedback = '';
    this.data.requestCustomerInformation(this.chargePointId, request).subscribe({
      next: result => {
        this.busy = false;
        if (result?.status === 'Accepted') this.feedback = 'Request accepted; the charger sends the data in one or more parts.';
        else this.error = result?.message || 'The charger did not accept the request.';
        this.page = 1;
        setTimeout(() => this.load(), 2000);
      },
      error: err => { this.busy = false; this.error = err?.error?.message || 'The request failed.'; }
    });
  }
}
