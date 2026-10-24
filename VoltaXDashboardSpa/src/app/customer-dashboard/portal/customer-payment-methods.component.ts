import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { followCheckout } from 'src/_helpers/download';
import { PaymentConfig, PaymentMethod } from 'src/_models/portal';
import { PaymentsService } from 'src/_services/portal/payments.service';
import { VxConfirmDialogComponent } from 'src/app/shared/portal-ui/portal-ui.module';

/** Saved payment cards: added on the provider's page, chosen as default or removed here. */
@Component({
  selector: 'app-customer-payment-methods',
  templateUrl: './customer-payment-methods.component.html'
})
export class CustomerPaymentMethodsComponent implements OnInit {
  private readonly payments = inject(PaymentsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  config: PaymentConfig | null = null;
  methods: PaymentMethod[] = [];
  loading = true;
  busy = false;
  error = false;
  setupResult: 'done' | 'canceled' | null = null;
  /** The card the user asked to remove, waiting for the confirmation. */
  pendingRemoval: PaymentMethod | null = null;
  @ViewChild('removeDialog') removeDialog?: VxConfirmDialogComponent;

  ngOnInit(): void {
    this.setupResult = this.route.snapshot.queryParamMap.get('setup') as 'done' | 'canceled' | null;
    this.payments.config().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: c => this.config = c, error: () => this.config = null });
    this.load();
  }

  load(): void {
    this.payments.methods().subscribe({
      next: methods => { this.methods = methods; this.loading = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  add(): void {
    this.busy = true;
    this.payments.startCardSetup().subscribe({
      next: checkout => followCheckout(checkout),
      error: () => { this.busy = false; this.error = true; }
    });
  }

  makeDefault(method: PaymentMethod): void {
    this.busy = true;
    this.payments.setDefault(method.id).subscribe({ next: () => { this.busy = false; this.load(); }, error: () => { this.busy = false; this.error = true; } });
  }

  askRemove(method: PaymentMethod): void {
    this.pendingRemoval = method;
    this.removeDialog?.open();
  }

  remove(): void {
    const method = this.pendingRemoval;
    this.pendingRemoval = null;
    if (!method) return;
    this.busy = true;
    this.payments.remove(method.id).subscribe({ next: () => { this.busy = false; this.load(); }, error: () => { this.busy = false; this.error = true; } });
  }

  expired(method: PaymentMethod): boolean {
    const now = new Date();
    return method.expiryYear < now.getFullYear() || (method.expiryYear === now.getFullYear() && method.expiryMonth < now.getMonth() + 1);
  }
}
