import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CheckoutInstruction, PaymentConfig, PaymentMethod, TopUpStatus } from 'src/_models/portal';

/** Online top-ups and saved cards (api/payments). Card numbers are only ever typed on the provider's page. */
@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly base = `${environment.apiUrl}/api/payments`;

  constructor(private http: HttpClient) {}

  config(): Observable<PaymentConfig> { return this.http.get<PaymentConfig>(`${this.base}/config`); }

  startTopUp(amount: number, cardId?: number | null): Observable<{ orderId: number; checkout: CheckoutInstruction }> {
    return this.http.post<{ orderId: number; checkout: CheckoutInstruction }>(`${this.base}/top-ups`, { amount, cardId: cardId ?? null });
  }

  topUp(orderId: number): Observable<TopUpStatus> { return this.http.get<TopUpStatus>(`${this.base}/top-ups/${orderId}`); }

  methods(): Observable<PaymentMethod[]> { return this.http.get<PaymentMethod[]>(`${this.base}/methods`); }

  startCardSetup(): Observable<CheckoutInstruction> { return this.http.post<CheckoutInstruction>(`${this.base}/methods/setup`, {}); }

  setDefault(id: number): Observable<void> { return this.http.post<void>(`${this.base}/methods/${id}/default`, {}); }

  remove(id: number): Observable<void> { return this.http.delete<void>(`${this.base}/methods/${id}`); }
}
