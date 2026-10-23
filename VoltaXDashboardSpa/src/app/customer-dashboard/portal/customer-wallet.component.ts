import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { filter, interval, startWith, switchMap, take, takeWhile } from 'rxjs';
import { followCheckout, saveDownload } from 'src/_helpers/download';
import { MyCard, MyOrder, MyWalletMovement, PagedResult, PaymentConfig, TopUpStatus } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { PaymentsService } from 'src/_services/portal/payments.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * Wallet: balance, online top-up (the card is typed on the payment provider's page), the result when the
 * customer comes back, the top-ups with their invoices and every balance movement.
 */
@Component({
  selector: 'app-customer-wallet',
  templateUrl: './customer-wallet.component.html'
})
export class CustomerWalletComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly payments = inject(PaymentsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly presets = [50, 100, 200, 500];
  config: PaymentConfig | null = null;
  cards: MyCard[] = [];
  cardId: number | null = null;
  amount: number | null = 100;
  starting = false;
  startError: string | null = null;
  returned: TopUpStatus | null = null;
  returnedCanceled = false;
  orders: PagedResult<MyOrder> | null = null;
  movements: PagedResult<MyWalletMovement> | null = null;
  readonly tone = statusTone;

  get balance(): number { return this.cards.reduce((sum, c) => sum + c.balance, 0); }
  get usableCards(): MyCard[] { return this.cards.filter(c => !c.blocked && !c.expired); }

  get amountError(): string | null {
    if (!this.config || this.amount == null) return null;
    if (this.amount < this.config.minTopUp || this.amount > this.config.maxTopUp) return 'customer.wallet.amountRange';
    return Math.round(this.amount * 100) !== this.amount * 100 ? 'customer.wallet.amountDecimals' : null;
  }

  ngOnInit(): void {
    this.payments.config().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: c => this.config = c, error: () => this.config = null });
    this.loadCards();
    this.loadOrders();
    this.loadMovements();

    // Back from the payment provider: follow the top-up until the provider's confirmation arrives.
    this.route.queryParamMap.pipe(
      filter(params => params.has('topup')),
      take(1),
      switchMap(params => {
        this.returnedCanceled = params.get('canceled') === 'true';
        const id = Number(params.get('topup'));
        return interval(2000).pipe(startWith(0), take(15), switchMap(() => this.payments.topUp(id)),
          takeWhile(status => status.status === 'Pending' || status.status === 'Processing', true));
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(status => {
      this.returned = status;
      if (status.status === 'Completed') { this.loadCards(); this.loadOrders(); this.loadMovements(); }
    });
  }

  loadCards(): void {
    this.portal.cards().subscribe(cards => {
      this.cards = cards;
      this.cardId ??= this.usableCards[0]?.id ?? null;
    });
  }

  loadOrders(page = 1): void { this.portal.orders(page, 5).subscribe(result => this.orders = result); }
  loadMovements(page = 1): void { this.portal.wallet(page, 10).subscribe(result => this.movements = result); }

  topUp(): void {
    if (this.amount == null || this.amountError) return;
    this.starting = true;
    this.startError = null;
    this.payments.startTopUp(this.amount, this.cardId).subscribe({
      next: started => followCheckout(started.checkout),
      error: response => {
        this.starting = false;
        this.startError = response?.error?.error ?? 'common.state.error';
      }
    });
  }

  dismissReturn(): void {
    this.returned = null;
    this.router.navigate([], { queryParams: {}, replaceUrl: true });
  }

  invoice(order: MyOrder): void {
    this.portal.orderInvoice(order.id).subscribe(response => saveDownload(response, `eveon-topup-${order.id}.pdf`));
  }

  print(): void { window.print(); }
}
