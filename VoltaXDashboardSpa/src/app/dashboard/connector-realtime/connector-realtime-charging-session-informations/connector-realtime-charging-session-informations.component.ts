import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-connector-realtime-charging-session-informations',
  templateUrl: './connector-realtime-charging-session-informations.component.html',
  styleUrls: ['./connector-realtime-charging-session-informations.component.sass']
})
export class ConnectorRealtimeChargingSessionInformationsComponent implements OnInit, OnDestroy {
  @Input() chargingSessionID = 0;

  readonly PageState = PageState;
  state = PageState.Loading;
  chargingSession: any = {};
  chargePointID = 0;
  vatOption = 'With VAT';
  isDownloading = false;
  private readonly destroyed$ = new Subject<void>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly chargingSessionService: ChargingSessionService
  ) {}

  ngOnInit(): void {
    this.chargePointID = Number(this.route.snapshot.paramMap.get('chargePointID')) || 0;
    this.chargingSessionID = Number(this.route.snapshot.paramMap.get('id')) || this.chargingSessionID;

    this.chargingSessionService.isDownloading$
      .pipe(takeUntil(this.destroyed$))
      .subscribe(status => this.isDownloading = status);

    if (!this.chargingSessionID) {
      this.state = PageState.NotFound;
      return;
    }

    this.loadChargingSession();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  loadChargingSession(): void {
    this.state = PageState.Loading;
    this.chargingSessionService.getChargingSessionInformations(this.chargingSessionID).subscribe({
      next: data => {
        this.chargingSession = data;
        this.state = data ? PageState.Success : PageState.NotFound;
      },
      error: error => this.state = error.status === 404 ? PageState.NotFound : PageState.Error
    });
  }

  getInvoice(): void {
    if (!this.isDownloading) {
      this.chargingSessionService.getChargingSessionInvoice(this.chargingSessionID);
    }
  }

  onToggleChange(option: string): void {
    this.vatOption = option;
  }

  get maskedCardNumber(): string {
    const cardNumber = String(this.chargingSession?.cardNumber || '');
    return cardNumber.length >= 4 ? `•••• ${cardNumber.slice(-4)}` : 'Not specified';
  }

  get chargingAmount(): number {
    return Number(this.vatOption === 'With VAT'
      ? this.chargingSession?.chargingPriceWithVAT
      : this.chargingSession?.chargingPriceWithoutVAT) || 0;
  }

  get idleAmount(): number {
    return Number(this.vatOption === 'With VAT'
      ? this.chargingSession?.idldePriceWithVAT
      : this.chargingSession?.idlePriceWithoutVAT) || 0;
  }

  get totalAmount(): number {
    return Number(this.vatOption === 'With VAT'
      ? this.chargingSession?.totalPriceWithVAT
      : this.chargingSession?.totalPriceWithoutVAT) || 0;
  }

  get vatAmount(): number {
    return Math.max(
      0,
      (Number(this.chargingSession?.totalPriceWithVAT) || 0)
        - (Number(this.chargingSession?.totalPriceWithoutVAT) || 0)
    );
  }
}
