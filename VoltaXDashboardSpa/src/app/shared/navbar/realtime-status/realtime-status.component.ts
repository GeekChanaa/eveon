import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Observable, debounceTime, distinctUntilChanged, filter, merge } from 'rxjs';
import { HubState, RealtimeHubService } from 'src/_services/realtime-hub.service';
import { VxI18nModule } from '../../i18n/vx-i18n.module';

/**
 * "Live updates" indicator of the header: hidden while the realtime connections are fine, "Reconnecting…" while one
 * is being restored, "Live updates paused" (click to retry now) once one gave up. Short blips are not shown.
 */
@Component({
  selector: 'app-realtime-status',
  standalone: true,
  imports: [CommonModule, VxI18nModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-container *ngIf="status$ | async as status">
      <button type="button" class="realtime-status" *ngIf="status === 'reconnecting' || status === 'disconnected'"
        [class.realtime-status--down]="status === 'disconnected'" (click)="retry()" role="status" aria-live="polite"
        [title]="('operator.realtime.' + status + 'Hint') | translate">
        <span class="realtime-status__dot" aria-hidden="true"></span>
        <span class="realtime-status__label">{{ ('operator.realtime.' + status) | translate }}</span>
      </button>
    </ng-container>`,
  styles: [`
    .realtime-status { display: inline-flex; align-items: center; gap: 6px; margin-inline-end: 12px; padding: 4px 10px; border: 1px solid #f0c36d;
      border-radius: 999px; background: #fff8e6; color: #8a5a00; font-size: 12px; font-weight: 600; line-height: 18px; cursor: pointer; white-space: nowrap; }
    .realtime-status--down { border-color: #f1a7a7; background: #fdecec; color: #a12626; }
    .realtime-status__dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; animation: realtime-pulse 1.2s ease-in-out infinite; }
    .realtime-status--down .realtime-status__dot { animation: none; }
    @keyframes realtime-pulse { 50% { opacity: .3; } }
    @media (max-width: 767px) { .realtime-status__label { display: none; } }
  `]
})
export class RealtimeStatusComponent {
  /** Healthy states show at once (hide the badge); trouble only after 1.5 s, so a quick reconnection stays invisible. */
  readonly status$: Observable<HubState>;

  constructor(private realtime: RealtimeHubService) {
    const healthy = (s: HubState) => s === 'connected' || s === 'idle' || s === 'connecting';
    this.status$ = merge(
      realtime.status$.pipe(filter(healthy)),
      realtime.status$.pipe(debounceTime(1500), filter(s => !healthy(s)))
    ).pipe(distinctUntilChanged());
  }

  retry(): void {
    this.realtime.retryNow();
  }
}
