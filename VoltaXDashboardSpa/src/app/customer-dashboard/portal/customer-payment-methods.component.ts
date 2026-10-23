import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { followCheckout } from 'src/_helpers/download';
import { PaymentConfig, PaymentMethod } from 'src/_models/portal';
import { LanguageService } from 'src/_services/language.service';
import { PaymentsService } from 'src/_services/portal/payments.service';

/** Saved payment cards: added on the provider's page, chosen as default or removed here. */
@Component({
  selector: 'app-customer-payment-methods',
  templateUrl: './customer-payment-methods.component.html'
})
export class CustomerPaymentMethodsComponent implements OnInit {
  private readonly payments = inject(PaymentsService);
  private readonly route = inject(ActivatedRoute);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);

  config: PaymentConfig | null = null;
  methods: PaymentMethod[] = [];
  loading = true;
  busy = false;
  error = false;
  setupResult: 'done' | 'canceled' | null = null;

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

  remove(method: PaymentMethod): void {
    if (!confirm(this.language.t('customer.methods.confirmRemove', { last4: method.last4 }))) return;
    this.busy = true;
    this.payments.remove(method.id).subscribe({ next: () => { this.busy = false; this.load(); }, error: () => { this.busy = false; this.error = true; } });
  }

  expired(method: PaymentMethod): boolean {
    const now = new Date();
    return method.expiryYear < now.getFullYear() || (method.expiryYear === now.getFullYear() && method.expiryMonth < now.getMonth() + 1);
  }
}
