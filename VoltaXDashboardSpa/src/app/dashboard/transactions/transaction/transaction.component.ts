import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TransactionService } from 'src/_services/transaction.service';

/** One OCPP transaction (by its charger-side id) with its meter readings and a link to the billed session. */
@Component({
  selector: 'app-transaction',
  template: `
    <div class="page__inner"><main class="page__container">
      <vx-print-header [title]="('ops.fields.transaction' | translate) + ' ' + uid"></vx-print-header>
      <header class="vx-heading">
        <div>
          <span class="vx-eyebrow">{{ 'ops.transactions.title' | translate }}</span>
          <h1 class="vx-mono">{{ uid }}</h1>
        </div>
        <div class="vx-actions no-print">
          <a class="vx-btn" routerLink="/dashboard/transactions">{{ 'common.actions.back' | translate }}</a>
          <a class="vx-btn vx-btn--primary" *ngIf="transaction" [routerLink]="['/dashboard/charging-sessions', transaction.chargingSessionID]">{{ 'ops.transactions.openSession' | translate }}</a>
          <button type="button" class="vx-btn" (click)="print()">{{ 'common.actions.print' | translate }}</button>
        </div>
      </header>
      <p class="vx-empty" *ngIf="loading">{{ 'common.state.loading' | translate }}</p>
      <div class="vx-alert vx-alert--error" *ngIf="notFound" role="alert">{{ 'ops.transactions.notFound' | translate }}</div>
      <section class="vx-panel" *ngIf="transaction as t">
        <dl class="vx-kv">
          <div><dt>{{ 'ops.fields.chargePoint' | translate }}</dt><dd>{{ t.chargePointID || '—' }}</dd></div>
          <div><dt>{{ 'ops.fields.connector' | translate }}</dt><dd>{{ t.connectorName || ('#' + t.connectorID) }}</dd></div>
          <div><dt>{{ 'ops.fields.session' | translate }}</dt><dd>#{{ t.chargingSessionID }}</dd></div>
          <div><dt>{{ 'common.fields.status' | translate }}</dt><dd>{{ 'ops.transactionStatus.' + (t.status ?? 'Ended') | translate }}</dd></div>
          <div><dt>{{ 'ops.fields.started' | translate }}</dt><dd>{{ t.startTime | vxDate:'medium' }}</dd></div>
          <div><dt>{{ 'ops.fields.stopped' | translate }}</dt><dd>{{ t.stopTime ? (t.stopTime | vxDate:'medium') : '—' }}</dd></div>
          <div><dt>{{ 'ops.fields.meterStart' | translate }}</dt><dd class="vx-num">{{ t.meterStart | vxNumber:'1.0-0' }} Wh</dd></div>
          <div><dt>{{ 'ops.fields.meterStop' | translate }}</dt><dd class="vx-num">{{ t.meterStop == null ? '—' : (t.meterStop | vxNumber:'1.0-0') + ' Wh' }}</dd></div>
          <div><dt>{{ 'ops.fields.energy' | translate }}</dt><dd class="vx-num">{{ energy == null ? '—' : (energy | vxNumber:'1.0-3') + ' ' + ('common.units.kwh' | translate) }}</dd></div>
          <div><dt>{{ 'ops.fields.idTag' | translate }}</dt><dd class="vx-mono">{{ t.startTagId || '—' }}</dd></div>
          <div><dt>{{ 'ops.fields.startResult' | translate }}</dt><dd>{{ t.startResult || '—' }}</dd></div>
          <div><dt>{{ 'ops.fields.stopReason' | translate }}</dt><dd>{{ t.stopReason || '—' }}</dd></div>
        </dl>
      </section>
    </main></div>`
})
export class TransactionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly transactions = inject(TransactionService);
  private readonly destroyRef = inject(DestroyRef);

  uid = '';
  transaction: any = null;
  loading = true;
  notFound = false;

  ngOnInit(): void {
    this.uid = this.route.snapshot.paramMap.get('id') ?? '';
    this.transactions.getTransactionByID(this.uid).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: t => { this.transaction = t; this.notFound = !t; this.loading = false; },
      error: () => { this.notFound = true; this.loading = false; }
    });
  }

  get energy(): number | null {
    const t = this.transaction;
    return t?.meterStop == null ? null : Math.max(0, t.meterStop - t.meterStart) / 1000;
  }

  print(): void { window.print(); }
}
