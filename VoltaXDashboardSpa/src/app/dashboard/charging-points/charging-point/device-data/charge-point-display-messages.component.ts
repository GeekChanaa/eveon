import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChargerDeviceDataService, LatestDisplayMessages } from 'src/_services/charger-device-data.service';
import { AccessService } from 'src/_services/access.service';

/** Latest display messages reported by the charger (GetDisplayMessages / NotifyDisplayMessages), with set and clear. */
@Component({
  selector: 'app-charge-point-display-messages',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['./device-data.sass'],
  template: `
    <div class="section-intro"><div><h2>Display messages</h2><p>Messages shown on the charger's display, as last reported by the charger.</p></div>
      <button type="button" class="secondary-action" *ngIf="canOperate" [disabled]="busy" (click)="refresh()">{{ busy ? 'Working...' : 'Refresh from charger' }}</button></div>
    <p class="device-feedback" role="status" *ngIf="feedback">{{ feedback }}</p>
    <p class="field-error" role="alert" *ngIf="error">{{ error }}</p>
    <p class="device-muted" *ngIf="latest?.receivedAt">Reported {{ latest!.receivedAt | date:'yyyy-MM-dd HH:mm:ss':'UTC' }} UTC (request {{ latest!.requestId }})</p>
    <div class="device-table">
      <table>
        <thead><tr><th>Id</th><th>Message</th><th>Priority</th><th>State</th><th>Window (UTC)</th><th>Display</th><th *ngIf="canOperate"></th></tr></thead>
        <tbody>
          <tr *ngIf="loading"><td colspan="7" class="device-empty">Loading...</td></tr>
          <tr *ngIf="!loading && !latest?.messages?.length"><td colspan="7" class="device-empty">{{ latest?.receivedAt ? 'The charger reported no display message.' : 'No report yet. Use "Refresh from charger".' }}</td></tr>
          <ng-container *ngIf="!loading">
            <tr *ngFor="let m of latest?.messages">
              <td>{{ m.messageId }}</td>
              <td><div class="device-content">{{ m.content }}</div><small>{{ m.format }}<span *ngIf="m.language"> &middot; {{ m.language }}</span><span *ngIf="m.transactionId"> &middot; transaction {{ m.transactionId }}</span></small></td>
              <td>{{ m.priority }}</td>
              <td>{{ m.state || 'Any' }}</td>
              <td><small>{{ (m.startDateTime | date:'yyyy-MM-dd HH:mm':'UTC') || 'now' }} &rarr; {{ (m.endDateTime | date:'yyyy-MM-dd HH:mm':'UTC') || 'no end' }}</small></td>
              <td>{{ m.displayComponentName || '-' }}<small *ngIf="m.displayEvseId"> EVSE {{ m.displayEvseId }}</small></td>
              <td *ngIf="canOperate"><button type="button" class="edit-button" [disabled]="busy" (click)="clear(m.messageId)">Clear</button></td>
            </tr>
          </ng-container>
        </tbody>
      </table>
    </div>
    <form class="device-form" *ngIf="canOperate" (ngSubmit)="set()">
      <h3>New message</h3>
      <label><span>Text</span><input name="content" [(ngModel)]="draft.content" maxlength="512" required></label>
      <label><span>Priority</span><select name="priority" [(ngModel)]="draft.priority"><option>NormalCycle</option><option>InFront</option><option>AlwaysFront</option></select></label>
      <label><span>Language</span><input name="language" [(ngModel)]="draft.language" maxlength="8" placeholder="en"></label>
      <label><span>Until (optional)</span><input type="datetime-local" name="end" [(ngModel)]="draft.end"></label>
      <div class="edit-actions"><button type="submit" class="primary-action" [disabled]="busy || !draft.content.trim()">Send to charger</button></div>
    </form>`
})
export class ChargePointDisplayMessagesComponent implements OnInit {
  @Input() chargePointId = '';
  latest?: LatestDisplayMessages;
  loading = false;
  busy = false;
  error = '';
  feedback = '';
  draft = { content: '', priority: 'NormalCycle', language: '', end: '' };

  constructor(private data: ChargerDeviceDataService, private access: AccessService) {}

  get canOperate(): boolean { return this.access.can('OperateChargePoints'); }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.data.latestDisplayMessages(this.chargePointId).subscribe({
      next: latest => { this.latest = latest; this.loading = false; },
      error: () => { this.loading = false; this.error = 'Could not load the display messages.'; }
    });
  }

  refresh(): void {
    this.run(this.data.refreshDisplayMessages(this.chargePointId), 'Request sent; the charger reports its messages in a moment.');
  }

  clear(id: number): void {
    this.run(this.data.clearDisplayMessage(this.chargePointId, id), 'Clear sent.');
  }

  set(): void {
    const message: any = {
      id: Math.floor(Math.random() * 1_000_000) + 1,
      priority: this.draft.priority,
      message: { format: 'UTF8', content: this.draft.content.trim(), ...(this.draft.language.trim() ? { language: this.draft.language.trim() } : {}) }
    };
    if (this.draft.end) message.endDateTime = new Date(this.draft.end).toISOString();
    this.run(this.data.setDisplayMessage(this.chargePointId, { message }), 'Message sent.', () => this.draft = { content: '', priority: 'NormalCycle', language: '', end: '' });
  }

  private run(request: ReturnType<ChargerDeviceDataService['refreshDisplayMessages']>, success: string, then?: () => void): void {
    this.busy = true;
    this.error = '';
    this.feedback = '';
    request.subscribe({
      next: result => {
        this.busy = false;
        const accepted = !result?.status || result.status === 'Accepted';
        if (accepted) { this.feedback = success; then?.(); } else this.error = result.message || `The charger answered ${result.status}.`;
        // The charger's NotifyDisplayMessages arrives right after its answer.
        setTimeout(() => this.load(), 2000);
      },
      error: err => { this.busy = false; this.error = err?.error?.message || 'The command failed.'; }
    });
  }
}
