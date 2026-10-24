import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargingSessionService } from 'src/_services/charging-session.service';

/** One line of the billing breakdown, VAT included. */
interface BillingLine { label: string; usage: string; rate: string | null; amount: number; }

/**
 * Operator view of a charging session: equipment, customer, timeline, the billed amounts (energy, time, session fee,
 * idle time, VAT) and the wallet balance before and after. Reached from the sessions list and from a charge point.
 */
@Component({
  selector: 'app-charging-session-detail',
  templateUrl: './charging-session-detail.component.html',
  styleUrls: ['./charging-session-detail.component.sass']
})
export class ChargingSessionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly chargingSessionService = inject(ChargingSessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly PageState = PageState;
  state = PageState.Loading;
  chargingSession: any = {};
  chargingSessionID = 0;
  /** Set when the page is opened from a charge point (connector realtime). */
  fromChargePointID = 0;
  isDownloading = false;

  ngOnInit(): void {
    this.fromChargePointID = Number(this.route.snapshot.paramMap.get('chargePointID')) || 0;
    this.chargingSessionID = Number(this.route.snapshot.paramMap.get('id')) || 0;
    this.chargingSessionService.isDownloading$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(status => this.isDownloading = status);
    if (!this.chargingSessionID) {
      this.state = PageState.NotFound;
      return;
    }
    this.load();
  }

  load(): void {
    this.state = PageState.Loading;
    this.chargingSessionService.getChargingSessionInformations(this.chargingSessionID).subscribe({
      next: data => {
        this.chargingSession = data ?? {};
        this.state = data ? PageState.Success : PageState.NotFound;
      },
      error: error => this.state = error.status === 404 ? PageState.NotFound : PageState.Error
    });
  }

  get chargePointID(): number { return this.fromChargePointID || Number(this.chargingSession?.chargePointID) || 0; }

  get breadcrumbs(): { label: string; link?: string }[] {
    const session = { label: 'Session #' + this.chargingSessionID };
    return this.fromChargePointID
      ? [{ label: 'Dashboard', link: '/dashboard' }, { label: 'Connector realtime', link: '/dashboard/connector-realtime' },
         { label: 'Charge point #' + this.fromChargePointID, link: '/dashboard/connector-realtime/' + this.fromChargePointID }, session]
      : [{ label: 'Dashboard', link: '/dashboard' }, { label: 'Charging sessions', link: '/dashboard/charging-sessions' }, session];
  }

  get active(): boolean { return !this.chargingSession?.endDate; }

  get maskedCardNumber(): string {
    const cardNumber = String(this.chargingSession?.cardNumber || '');
    return cardNumber.length >= 4 ? `•••• ${cardNumber.slice(-4)}` : 'Not specified';
  }

  /** What the driver paid, line by line. Sessions billed before the amounts were stored only have a time and idle line. */
  get lines(): BillingLine[] {
    const s = this.chargingSession ?? {};
    const n = (v: any) => Number(v) || 0;
    const lines: BillingLine[] = [];
    if (s.isBilled) {
      if (n(s.energyPriceWithVAT) || n(s.pricePerKWh))
        lines.push({ label: 'Energy', usage: `${n(s.kwhCharged).toFixed(2)} kWh`, rate: `${n(s.pricePerKWh).toFixed(2)} DH/kWh`, amount: n(s.energyPriceWithVAT) });
      if (n(s.timePriceWithVAT) || n(s.pricePerMinute))
        lines.push({ label: 'Charging time', usage: `${n(s.chargedMinutes).toFixed(1)} min`, rate: `${n(s.pricePerMinute).toFixed(2)} DH/min`, amount: n(s.timePriceWithVAT) });
      if (n(s.flatFeeWithVAT))
        lines.push({ label: 'Session fee', usage: '1', rate: null, amount: n(s.flatFeeWithVAT) });
    } else {
      lines.push({ label: 'Charging time', usage: `${n(s.chargedMinutes).toFixed(1)} min`, rate: `${n(s.pricePerMinute).toFixed(2)} DH/min`, amount: n(s.chargingPriceWithVAT) });
    }
    if (n(s.idldePriceWithVAT) > 0)
      lines.push({ label: 'Idle time', usage: `${n(s.idleMinutes).toFixed(1)} min`, rate: `${n(s.pricePerIdleMinute).toFixed(2)} DH/min`, amount: n(s.idldePriceWithVAT) });
    return lines;
  }

  get total(): number { return Number(this.chargingSession?.totalPriceWithVAT) || 0; }
  get totalExclVat(): number { return Number(this.chargingSession?.totalPriceWithoutVAT) || 0; }
  get vat(): number { return Math.max(0, this.total - this.totalExclVat); }
  get vatPercent(): number { return Math.round((Number(this.chargingSession?.vatRate) || 0) * 1000) / 10; }

  getInvoice(): void {
    if (!this.isDownloading) this.chargingSessionService.getChargingSessionInvoice(this.chargingSessionID);
  }

  print(): void { window.print(); }
}
