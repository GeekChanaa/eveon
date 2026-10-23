import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VxI18nModule } from 'src/app/shared/i18n/vx-i18n.module';
import { AccessService } from 'src/_services/access.service';
import { BillingService } from 'src/_services/billing/billing.service';
import { LanguageService } from 'src/_services/language.service';
import { Refund, RefundMethod, RefundSummary, RefundTarget, newIdempotencyKey } from 'src/_models/billing';

/**
 * Refunds of a top-up or a charging session on its operator page: what was paid, refunded and is left, the
 * refunds made, and (with EditRechargeOrders) a refund form with a confirmation step. One idempotency key per
 * confirmed refund: retrying after a network error never refunds twice.
 */
@Component({
  selector: 'vx-refund-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, VxI18nModule],
  template: `
    <section class="vx-panel vx-refunds no-print" *ngIf="summary">
      <div class="vx-panel__head">
        <div>
          <h2>{{ 'billing.refunds.title' | translate }}</h2>
          <p>{{ (target === 'Order' ? 'billing.refunds.orderHint' : 'billing.refunds.sessionHint') | translate }}</p>
        </div>
        <button type="button" class="vx-btn vx-btn--primary" *ngIf="canRefund && step === 'idle'" (click)="open()">
          {{ 'billing.refunds.refund' | translate }}
        </button>
      </div>

      <dl class="vx-kv">
        <div><dt>{{ 'billing.refunds.paid' | translate }}</dt><dd>{{ summary.paid | vxMoney }}</dd></div>
        <div><dt>{{ 'billing.refunds.refunded' | translate }}</dt><dd>{{ summary.refunded | vxMoney }}</dd></div>
        <div><dt>{{ 'billing.refunds.refundable' | translate }}</dt><dd><strong>{{ summary.refundable | vxMoney }}</strong></dd></div>
        <div *ngIf="target === 'Order'"><dt>{{ 'billing.refunds.walletBalance' | translate }}</dt><dd>{{ summary.cardBalance | vxMoney }}</dd></div>
      </dl>
      <p class="vx-footnote" *ngIf="noteKey">{{ noteKey | translate:{ provider: summary.provider } }}</p>

      <form class="vx-refund-form" *ngIf="step === 'form'" (ngSubmit)="review()" novalidate>
        <div class="vx-field">
          <label for="refund-amount">{{ 'billing.refunds.amount' | translate }}</label>
          <input id="refund-amount" name="amount" type="number" inputmode="decimal" min="0.01" step="0.01" [max]="summary.refundable"
            [(ngModel)]="amount" required>
          <button type="button" class="vx-btn vx-btn--link" (click)="amount = summary.refundable">{{ 'billing.refunds.all' | translate }}</button>
        </div>
        <div class="vx-field" *ngIf="summary.methods.length > 1">
          <label for="refund-method">{{ 'billing.refunds.method' | translate }}</label>
          <select id="refund-method" name="method" [(ngModel)]="method">
            <option *ngFor="let m of summary.methods" [ngValue]="m">{{ 'billing.refunds.methods.' + m | translate:{ provider: summary.provider } }}</option>
          </select>
        </div>
        <div class="vx-field vx-field--grow">
          <label for="refund-reason">{{ 'billing.refunds.reason' | translate }}</label>
          <textarea id="refund-reason" name="reason" rows="2" maxlength="256" [(ngModel)]="reason" required></textarea>
        </div>
        <p class="vx-field__error" *ngIf="formError" role="alert">{{ formError | translate:{ max: (summary.refundable | vxMoney) } }}</p>
        <div class="vx-actions">
          <button type="submit" class="vx-btn vx-btn--primary">{{ 'billing.refunds.continue' | translate }}</button>
          <button type="button" class="vx-btn" (click)="cancel()">{{ 'common.actions.cancel' | translate }}</button>
        </div>
      </form>

      <div class="vx-alert" *ngIf="step === 'confirm'" role="alertdialog" aria-live="assertive">
        <p>{{ 'billing.refunds.confirm' | translate:{ amount: (amount | vxMoney), method: ('billing.refunds.methods.' + method | translate:{ provider: summary.provider }) } }}</p>
        <p class="muted">{{ 'billing.refunds.irreversible' | translate }}</p>
        <div class="vx-actions">
          <button type="button" class="vx-btn vx-btn--danger" (click)="submit()" [disabled]="saving">
            {{ (saving ? 'billing.refunds.refunding' : 'billing.refunds.confirmButton') | translate }}
          </button>
          <button type="button" class="vx-btn" (click)="step = 'form'" [disabled]="saving">{{ 'common.actions.back' | translate }}</button>
        </div>
      </div>

      <div class="vx-alert vx-alert--success" *ngIf="done" role="status">
        {{ 'billing.refunds.done' | translate:{ amount: (done.amount | vxMoney), creditNote: done.creditNoteNumber || '—' } }}
      </div>
      <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ error }}</div>

      <div class="vx-table-wrap" *ngIf="summary.refunds.length">
        <table class="vx-table">
          <thead><tr>
            <th>{{ 'billing.refunds.date' | translate }}</th>
            <th class="num">{{ 'billing.refunds.amount' | translate }}</th>
            <th>{{ 'billing.refunds.method' | translate }}</th>
            <th>{{ 'common.fields.status' | translate }}</th>
            <th>{{ 'billing.refunds.creditNote' | translate }}</th>
            <th>{{ 'billing.refunds.reason' | translate }}</th>
          </tr></thead>
          <tbody>
            <tr *ngFor="let r of summary.refunds">
              <td>{{ r.createdAt | vxDate:'short' }}<div class="muted" *ngIf="r.requestedBy">{{ r.requestedBy }}</div></td>
              <td class="num">{{ r.amount | vxMoney }}</td>
              <td>{{ 'billing.refunds.methods.' + r.method | translate:{ provider: r.provider || summary.provider } }}</td>
              <td><span class="vx-badge" [ngClass]="tone(r)">{{ 'billing.refunds.status.' + r.status | translate }}</span>
                <div class="muted" *ngIf="r.failureReason">{{ r.failureReason }}</div></td>
              <td class="vx-mono">{{ r.creditNoteNumber || '—' }}</td>
              <td>{{ r.reason }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>`,
  styles: [`
    .vx-refunds { margin-block-start: 24px; }
    .vx-refunds h2 { font-size: 16px; font-weight: 650; margin: 0 0 4px; }
    .vx-refunds p { margin: 0; }
    .vx-refund-form { display: flex; flex-wrap: wrap; gap: 12px 16px; align-items: flex-end; margin-block: 16px; }
    .vx-refund-form .vx-field--grow { flex: 1 1 280px; }
    .vx-refunds .vx-alert { margin-block: 12px; }
    .vx-refunds .muted { color: var(--vx-text-muted); font-size: 12px; }
  `]
})
export class RefundPanelComponent implements OnChanges {
  @Input() target: RefundTarget = 'Order';
  @Input() targetId = 0;

  summary: RefundSummary | null = null;
  step: 'idle' | 'form' | 'confirm' = 'idle';
  amount: number | null = null;
  method: RefundMethod | null = null;
  reason = '';
  formError: string | null = null;
  error: string | null = null;
  done: Refund | null = null;
  saving = false;
  private key = '';

  constructor(private billing: BillingService, private access: AccessService, private language: LanguageService) {}

  get canRefund(): boolean {
    return !!this.summary && this.summary.refundable > 0 && this.summary.methods.length > 0 && this.access.can('EditRechargeOrders');
  }

  /** Explains when the money cannot go back through the payment provider. */
  get noteKey(): string | null {
    if (!this.summary || this.target !== 'Order' || this.summary.paid <= 0 || this.summary.methods.includes('Gateway')) return null;
    return !this.summary.provider || this.summary.provider === 'Manual' ? 'billing.refunds.notes.manual' : 'billing.refunds.notes.provider';
  }

  ngOnChanges(): void {
    if (this.targetId) this.load();
  }

  load(): void {
    this.billing.refunds(this.target, this.targetId).subscribe({
      next: summary => this.summary = summary,
      error: () => this.summary = null // no permission or not found: the panel stays hidden
    });
  }

  open(): void {
    this.done = null;
    this.error = null;
    this.formError = null;
    this.amount = this.summary?.refundable ?? null;
    this.method = this.summary?.defaultMethod ?? null;
    this.reason = '';
    this.key = newIdempotencyKey();
    this.step = 'form';
  }

  cancel(): void {
    this.step = 'idle';
    this.formError = null;
  }

  /** Checks the form, then asks for confirmation. */
  review(): void {
    this.formError = this.validate();
    if (!this.formError) this.step = 'confirm';
  }

  submit(): void {
    if (this.saving || this.validate()) return;
    this.saving = true;
    this.error = null;
    this.billing.refund(this.target, this.targetId, { amount: Number(this.amount), reason: this.reason.trim(), idempotencyKey: this.key, method: this.method })
      .subscribe({
        next: refund => {
          this.saving = false;
          this.done = refund;
          this.step = 'idle';
          this.load();
        },
        error: response => {
          this.saving = false;
          const code: string | undefined = response?.error?.code;
          const translated = code ? this.language.t('billing.refunds.errors.' + code) : '';
          this.error = translated && !translated.startsWith('billing.') ? translated : response?.error?.error || this.language.t('common.state.error');
          // The key is kept: retrying the same refund cannot refund twice. A refused refund needs a new form.
          if (response?.status && response.status < 500) this.step = 'form';
          if (response?.status === 502) { this.step = 'idle'; this.load(); }
        }
      });
  }

  tone(refund: Refund): string {
    return refund.status === 'Succeeded' ? 'vx-badge--success' : refund.status === 'Failed' ? 'vx-badge--danger' : 'vx-badge--warning';
  }

  private validate(): string | null {
    const amount = Number(this.amount);
    if (!this.summary || !Number.isFinite(amount) || amount <= 0 || amount > this.summary.refundable) return 'billing.refunds.amountRange';
    if (Math.round(amount * 100) !== amount * 100) return 'billing.refunds.amountCents';
    if (this.reason.trim().length < 3) return 'billing.refunds.reasonRequired';
    return null;
  }
}
