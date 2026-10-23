import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, forkJoin, timer } from 'rxjs';
import { PartnerChargePoint, PartnerConnector, PartnerStation } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { VxConfirmDialogComponent, connectorTypeLabel, statusTone } from 'src/app/shared/portal-ui/portal-ui.module';
import { CommandAnswer, CommandFeedback, commandFeedback, feedbackClass } from 'src/app/shared/portal-ui/remote-command';
import { PricingChange } from './partner-pricing-editor.component';

/** A remote action waiting for the partner's confirmation. */
export interface PendingAction {
  kind: 'softReset' | 'hardReset' | 'unlock' | 'stop';
  point: PartnerChargePoint;
  connector?: PartnerConnector;
}

/** The feedback key family (portal.command.{x}) of each action. */
const FEEDBACK: Record<PendingAction['kind'] | 'refresh', string> = {
  softReset: 'reset', hardReset: 'reset', unlock: 'unlock', stop: 'sessionStop', refresh: 'refresh'
};

/**
 * The partner's charge points: connection, live connector state and prices (editable), and the remote actions on
 * them: refresh the status, restart (once idle or now), unlock a connector, stop the session running on it. Every
 * action that interrupts charging asks for a confirmation and the charger's answer is shown on the charge point.
 */
@Component({
  selector: 'app-partner-charge-points',
  templateUrl: './partner-charge-points.component.html'
})
export class PartnerChargePointsComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('actionDialog') actionDialog?: VxConfirmDialogComponent;

  stations: PartnerStation[] = [];
  chargePoints: PartnerChargePoint[] = [];
  stationId: number | null = null;
  loading = true;
  error = false;
  editing: number | null = null;
  saving = false;
  saveError: string | null = null;
  savedConnectors = new Set<number>();
  pending: PendingAction | null = null;
  /** Charge points with a command on its way. */
  readonly busy = new Set<number>();
  /** The last command's result, per charge point. */
  feedback: Record<number, CommandFeedback> = {};
  readonly tone = statusTone;
  readonly typeLabel = connectorTypeLabel;
  readonly feedbackClass = feedbackClass;

  ngOnInit(): void {
    this.portal.stations().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: stations => this.stations = stations, error: () => {} });
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const station = Number(params.get('station'));
      this.stationId = Number.isInteger(station) && station > 0 ? station : null;
      this.load();
    });
  }

  load(): void {
    this.loading = true;
    this.portal.chargePoints(this.stationId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: points => { this.chargePoints = points; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  selectStation(id: string): void {
    this.editing = null;
    this.router.navigate([], { relativeTo: this.route, queryParams: { station: id || null } });
  }

  edit(connector: PartnerConnector): void {
    this.editing = connector.id;
    this.saveError = null;
  }

  save(point: PartnerChargePoint, connector: PartnerConnector, change: PricingChange): void {
    const targets = change.applyToAll ? point.connectors : [connector];
    this.saving = true;
    this.saveError = null;
    forkJoin(targets.map(c => this.portal.updatePricing(c.id, change.pricing))).subscribe({
      next: updated => {
        point.connectors = point.connectors.map(c => updated.find(u => u.id === c.id) ?? c);
        this.savedConnectors = new Set(updated.map(u => u.id));
        this.saving = false;
        this.editing = null;
      },
      error: err => {
        this.saving = false;
        this.saveError = err?.status === 400 ? 'partner.pricing.invalid' : 'partner.pricing.failed';
      }
    });
  }

  // ------------------------------------------------------------------ remote actions

  /** Asks the charger for the status of its connectors (harmless: no confirmation). */
  refresh(point: PartnerChargePoint): void {
    this.send(point, 'refresh', this.portal.refreshStatus(point.id));
  }

  /** Opens the confirmation of an action that affects drivers. */
  ask(kind: PendingAction['kind'], point: PartnerChargePoint, connector?: PartnerConnector): void {
    this.pending = { kind, point, connector };
    this.actionDialog?.open();
  }

  confirmPending(): void {
    const action = this.pending;
    this.pending = null;
    if (!action) return;
    const request = action.kind === 'softReset' ? this.portal.reset(action.point.id, 'Soft')
      : action.kind === 'hardReset' ? this.portal.reset(action.point.id, 'Hard')
      : action.kind === 'unlock' ? this.portal.unlock(action.connector!.id)
      : this.portal.stopSession(action.connector!.activeSessionId!);
    this.send(action.point, action.kind, request);
  }

  /** Sessions running on the charge point (a hard restart stops them). */
  running(point: PartnerChargePoint): number {
    return point.connectors.filter(c => c.activeSessionId != null).length;
  }

  dismiss(chargePointId: number): void { delete this.feedback[chargePointId]; }

  print(): void { window.print(); }

  connectorLabel(c: PartnerConnector): string {
    return c.connectorId == null || c.connectorId === 1 ? `EVSE ${c.evseId}` : `EVSE ${c.evseId} / ${c.connectorId}`;
  }

  /** "ocpp2.0.1" → "OCPP 2.0.1". */
  protocolLabel(protocol: string | null): string {
    return protocol ? protocol.replace(/^ocpp/i, 'OCPP ') : '';
  }

  private send(point: PartnerChargePoint, kind: PendingAction['kind'] | 'refresh', request: Observable<CommandAnswer>): void {
    this.busy.add(point.id);
    delete this.feedback[point.id];
    request.subscribe({
      next: answer => {
        this.busy.delete(point.id);
        this.feedback[point.id] = commandFeedback(answer, FEEDBACK[kind]);
        // The charger reports its new state with StatusNotifications: show it once they had time to arrive.
        if (this.feedback[point.id].tone === 'success') timer(4000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
      },
      error: (error: HttpErrorResponse) => {
        this.busy.delete(point.id);
        this.feedback[point.id] = commandFeedback(error, FEEDBACK[kind]);
      }
    });
  }
}
