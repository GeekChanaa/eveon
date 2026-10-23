import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { MyActiveSession } from 'src/_models/portal';
import { LiveConnectionState, LiveSessionUpdate, PortalLiveSessionService } from 'src/_services/portal/live-session.service';
import { apiDate } from 'src/app/shared/i18n/vx-i18n.module';
import { CommandFeedback, commandFeedback, feedbackClass } from 'src/app/shared/portal-ui/remote-command';
import { VxConfirmDialogComponent, elapsedClock } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * The customer's running session, live: energy, power, duration, cost so far and what is left on the card,
 * pushed by the API as the charger reports (a refresh of the session snapshot from the page also counts), and a
 * remote stop of the session (the customer's own; the API checks it again). Emits ended when the session is over.
 */
@Component({
  selector: 'app-customer-live-session',
  templateUrl: './customer-live-session.component.html'
})
export class CustomerLiveSessionComponent implements OnInit, OnDestroy {
  private readonly live = inject(PortalLiveSessionService);
  private readonly destroyRef = inject(DestroyRef);

  private snapshot!: MyActiveSession;
  @Input() set session(value: MyActiveSession) {
    if (this.snapshot?.id !== value.id) { this.update = null; this.feedback = null; this.stopping = false; }
    this.snapshot = value;
  }
  get session(): MyActiveSession { return this.snapshot; }
  /** Compact layout (session detail page): no link to the session itself. */
  @Input() compact = false;
  @Output() ended = new EventEmitter<LiveSessionUpdate>();

  @ViewChild('stopDialog') stopDialog?: VxConfirmDialogComponent;

  update: LiveSessionUpdate | null = null;
  connection: LiveConnectionState = 'idle';
  now = Date.now();
  stopping = false;
  feedback: CommandFeedback | null = null;
  readonly feedbackClass = feedbackClass;

  ngOnInit(): void {
    this.live.follow();
    this.live.state$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(state => this.connection = state);
    this.live.updates$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(update => this.receive(update));
    interval(1000).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.now = Date.now());
  }

  ngOnDestroy(): void { this.live.release(); }

  /** Energy delivered so far (the newest of the snapshot and the last push: both only grow). */
  get energy(): number { return Math.max(this.session.chargedKwhs, this.update?.energyKWh ?? 0); }

  get cost(): number { return Math.max(this.session.currentCost, this.update?.currentCost ?? 0); }

  /** Power between the last two meter readings; the session average until the charger has sent two. */
  get power(): number | null {
    if (this.update?.powerKw != null) return this.update.powerKw;
    const minutes = Math.max(this.session.chargedMinutes, this.update?.durationMinutes ?? 0);
    return minutes >= 1 ? this.energy / (minutes / 60) : null;
  }

  get powerIsAverage(): boolean { return this.update?.powerKw == null && this.power != null; }

  get clock(): string { return elapsedClock(this.now - apiDate(this.session.startDate).getTime()); }

  /** What is left on the card once this session's cost so far is paid. */
  get remaining(): number | null {
    return this.session.cardBalance == null ? null : Math.max(0, this.session.cardBalance - this.cost);
  }

  get usedPercent(): number {
    const balance = this.session.cardBalance;
    return balance ? Math.min(100, Math.round(this.cost * 100 / balance)) : 0;
  }

  get lowBalance(): boolean { return this.update?.status === 'StoppingLowBalance'; }

  get canStop(): boolean { return !!this.session.transactionUid && !!this.session.chargePointName && !this.stopping; }

  get connectionTone(): string {
    return this.connection === 'live' ? 'vx-badge--success' : this.connection === 'offline' ? 'vx-badge--danger' : 'vx-badge--warning';
  }

  askStop(): void {
    this.feedback = null;
    this.stopDialog?.open();
  }

  stop(): void {
    if (!this.canStop) return;
    this.stopping = true;
    this.feedback = null;
    this.live.stop(this.session.chargePointName!, this.session.transactionUid!).subscribe({
      next: answer => {
        this.feedback = commandFeedback(answer, 'stop');
        // Accepted: the charger ends the transaction, the Ended update follows. Otherwise the session goes on.
        this.stopping = this.feedback.tone === 'success';
      },
      error: (error: HttpErrorResponse) => {
        this.stopping = false;
        this.feedback = commandFeedback(error, 'stop');
      }
    });
  }

  private receive(update: LiveSessionUpdate): void {
    if (update.sessionId !== this.session.id) return;
    this.update = update;
    if (update.status === 'Ended') {
      this.stopping = false;
      this.ended.emit(update);
    }
  }
}
