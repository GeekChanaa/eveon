import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, NgModule, Output, Pipe, PipeTransform } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LanguageService } from 'src/_services/language.service';
import { VxI18nModule } from '../i18n/vx-i18n.module';
import { VxConfirmDialogComponent } from './vx-confirm-dialog.component';
import { VxPrintFooterComponent, VxPrintHeaderComponent } from './vx-print.components';

export { VxConfirmDialogComponent } from './vx-confirm-dialog.component';
export { VxPrintFooterComponent, VxPrintHeaderComponent } from './vx-print.components';

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

/** An icon of the dashboard's SVG sprite (src/index.html), decorative: <vx-icon name="lightning"></vx-icon>. */
@Component({
  selector: 'vx-icon',
  template: `<svg class="vx-icon" aria-hidden="true" focusable="false"><use [attr.href]="'#icon-' + name"></use></svg>`
})
export class VxIconComponent {
  @Input() name = 'info';
}

/** A duration given in minutes, in the current language: "45 min", "2 h 05 min". */
@Pipe({ name: 'vxDuration', pure: false })
export class VxDurationPipe implements PipeTransform {
  constructor(private language: LanguageService) {}

  transform(minutes: number | null | undefined): string {
    if (minutes == null || !isFinite(minutes)) return '—';
    const total = Math.max(0, Math.round(minutes));
    if (total < 60) return this.language.t('portal.duration.minutes', { m: total });
    return this.language.t('portal.duration.hours', { h: Math.floor(total / 60), m: String(total % 60).padStart(2, '0') });
  }
}

/** A running clock, "1:05:09" or "12:30", from milliseconds (digits stay left to right in Arabic). */
export function elapsedClock(milliseconds: number): string {
  const seconds = Math.max(0, Math.floor(milliseconds / 1000));
  const h = Math.floor(seconds / 3600);
  const m = Math.floor(seconds / 60) % 60;
  const s = seconds % 60;
  const two = (n: number) => String(n).padStart(2, '0');
  return h > 0 ? `${h}:${two(m)}:${two(s)}` : `${m}:${two(s)}`;
}

const PORTAL_UI = [VxPagerComponent, VxPrintHeaderComponent, VxPrintFooterComponent, VxConfirmDialogComponent, VxIconComponent, VxDurationPipe];

/** Everything a portal page needs: common modules, translations, pager, print header / footer, dialogs, icons. */
@NgModule({
  declarations: PORTAL_UI,
  imports: [CommonModule, VxI18nModule],
  exports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, VxI18nModule, ...PORTAL_UI]
})
export class PortalUiModule {}

/** Badge class of a session / order / connector / command status. */
export function statusTone(status: string | null | undefined): string {
  switch (status) {
    case 'Completed': case 'Available': case 'Accepted': case 'Active': case 'Unlocked': case 'Online': return 'vx-badge--success';
    case 'Pending': case 'Processing': case 'CompletedOccupied': case 'Occupied': case 'Reserved': case 'Scheduled': case 'UnderMaintenance':
      return 'vx-badge--warning';
    case 'Failed': case 'Faulted': case 'Canceled': case 'Blocked': case 'Expired': case 'Unavailable': case 'Rejected': case 'UnlockFailed':
    case 'Offline': return 'vx-badge--danger';
    default: return 'vx-badge--info';
  }
}

const CONNECTOR_TYPES: Record<string, string> = {
  cCCS1: 'CCS1', cCCS2: 'CCS2', cG105: 'CHAdeMO', cTesla: 'Tesla', cType1: 'Type 1', cType2: 'Type 2',
  s309_1P_16A: 'IEC 60309 1P 16A', s309_1P_32A: 'IEC 60309 1P 32A', s309_3P_16A: 'IEC 60309 3P 16A', s309_3P_32A: 'IEC 60309 3P 32A',
  sBS1361: 'BS 1361', sCEE_7_7: 'Schuko', sType2: 'Type 2 (socket)', sType3: 'Type 3', Pan: 'Pantograph', wInductive: 'Inductive',
  wResonant: 'Wireless', Undetermined: '—', Unknown: '—'
};

/** "First Last" of a person, null when unknown (printed headers). */
export function fullName(person: { firstName?: string | null; lastName?: string | null } | null | undefined): string | null {
  return person ? `${person.firstName ?? ''} ${person.lastName ?? ''}`.trim() || null : null;
}

/** Human name of an OCPP connector type (cCCS2 → CCS2). */
export function connectorTypeLabel(type: string | null | undefined): string {
  return type ? CONNECTOR_TYPES[type] ?? type : '—';
}
