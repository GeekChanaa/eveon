import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, NgModule, Output } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { VxI18nModule } from '../i18n/vx-i18n.module';

/** Previous / next pager with the result count, for the portal tables. */
@Component({
  selector: 'vx-pager',
  template: `
    <nav class="vx-pager no-print" *ngIf="total > 0" [attr.aria-label]="'common.pager.results' | translate:{ total: total }">
      <span>{{ 'common.pager.results' | translate:{ total: total } }} · {{ 'common.pager.page' | translate:{ page: page, pages: pages } }}</span>
      <span class="vx-actions">
        <button type="button" class="vx-btn vx-btn--small" [disabled]="page <= 1" (click)="go(page - 1)">{{ 'common.actions.previous' | translate }}</button>
        <button type="button" class="vx-btn vx-btn--small" [disabled]="page >= pages" (click)="go(page + 1)">{{ 'common.actions.next' | translate }}</button>
      </span>
    </nav>`
})
export class VxPagerComponent {
  @Input() page = 1;
  @Input() pageSize = 20;
  @Input() total = 0;
  @Output() pageChange = new EventEmitter<number>();

  get pages(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  go(page: number): void {
    if (page >= 1 && page <= this.pages) this.pageChange.emit(page);
  }
}

/** Brand header shown only on paper (window.print()). */
@Component({
  selector: 'vx-print-header',
  template: `
    <header class="vx-print-header print-only">
      <img src="/assets/images/logos/logo.svg" alt="eveon">
      <img *ngIf="partnerLogo" class="vx-print-header__partner" [src]="partnerLogo" alt="">
      <div><strong>{{ title }}</strong><span *ngIf="subtitle">{{ subtitle }} · </span>{{ printedAt | vxDate:'medium' }}</div>
    </header>`
})
export class VxPrintHeaderComponent {
  @Input() title = '';
  @Input() subtitle: string | null = null;
  /** A partner's logo, printed next to eveon's on partner portal pages. */
  @Input() partnerLogo: string | null = null;
  readonly printedAt = new Date();
}

/** Everything a portal page needs: common modules, translations, pager, print header. */
@NgModule({
  declarations: [VxPagerComponent, VxPrintHeaderComponent],
  imports: [CommonModule, VxI18nModule],
  exports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, VxI18nModule, VxPagerComponent, VxPrintHeaderComponent]
})
export class PortalUiModule {}

/** Badge class of a session / order / connector status. */
export function statusTone(status: string | null | undefined): string {
  switch (status) {
    case 'Completed': case 'Available': case 'Accepted': case 'Active': return 'vx-badge--success';
    case 'Pending': case 'Processing': case 'CompletedOccupied': case 'Occupied': case 'Reserved': return 'vx-badge--warning';
    case 'Failed': case 'Faulted': case 'Canceled': case 'Blocked': case 'Expired': case 'Unavailable': return 'vx-badge--danger';
    default: return 'vx-badge--info';
  }
}

const CONNECTOR_TYPES: Record<string, string> = {
  cCCS1: 'CCS1', cCCS2: 'CCS2', cG105: 'CHAdeMO', cTesla: 'Tesla', cType1: 'Type 1', cType2: 'Type 2',
  s309_1P_16A: 'IEC 60309 1P 16A', s309_1P_32A: 'IEC 60309 1P 32A', s309_3P_16A: 'IEC 60309 3P 16A', s309_3P_32A: 'IEC 60309 3P 32A',
  sBS1361: 'BS 1361', sCEE_7_7: 'Schuko', sType2: 'Type 2 (socket)', sType3: 'Type 3', Pan: 'Pantograph', wInductive: 'Inductive',
  wResonant: 'Wireless', Undetermined: '—', Unknown: '—'
};

/** Human name of an OCPP connector type (cCCS2 → CCS2). */
export function connectorTypeLabel(type: string | null | undefined): string {
  return type ? CONNECTOR_TYPES[type] ?? type : '—';
}
