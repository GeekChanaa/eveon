import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import { PagedResult } from 'src/_models/portal';
import {
  InvoiceFilter, InvoiceRow, PartnerCommission, Refund, RefundRequest, RefundSummary, RefundTarget, Settlement, SettlementDetail,
  SettlementFilter
} from 'src/_models/billing';

/** Operator billing API: refunds (api/Refund), the invoice register (api/Invoice) and partner settlements (api/Settlement). */
@Injectable({ providedIn: 'root' })
export class BillingService {
  private readonly api = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  refunds(target: RefundTarget, id: number): Observable<RefundSummary> {
    return this.http.get<RefundSummary>(`${this.api}/Refund/${target}/${id}`);
  }

  refund(target: RefundTarget, id: number, request: RefundRequest): Observable<Refund> {
    return this.http.post<Refund>(`${this.api}/Refund/${target}/${id}`, request);
  }

  invoices(filter: InvoiceFilter): Observable<PagedResult<InvoiceRow>> {
    return this.http.get<PagedResult<InvoiceRow>>(`${this.api}/Invoice/Search`, { params: toParams(filter) });
  }

  invoicePdf(id: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.api}/Invoice/${id}/Pdf`, { observe: 'response', responseType: 'blob' });
  }

  invoiceJournal(filter: InvoiceFilter): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.api}/Invoice/Journal`, { params: toParams(rest), observe: 'response', responseType: 'blob' });
  }

  commissions(): Observable<PartnerCommission[]> {
    return this.http.get<PartnerCommission[]>(`${this.api}/Settlement/Commissions`);
  }

  updateCommission(partnerId: number, commissionPercent: number, commissionPerKwh: number): Observable<PartnerCommission> {
    return this.http.put<PartnerCommission>(`${this.api}/Settlement/Commissions/${partnerId}`, { commissionPercent, commissionPerKwh });
  }

  settlements(filter: SettlementFilter): Observable<PagedResult<Settlement>> {
    return this.http.get<PagedResult<Settlement>>(`${this.api}/Settlement/Search`, { params: toParams(filter) });
  }

  settlement(id: number): Observable<SettlementDetail> {
    return this.http.get<SettlementDetail>(`${this.api}/Settlement/${id}`);
  }

  generate(year: number, month: number, partnerId?: number | null): Observable<Settlement[]> {
    return this.http.post<Settlement[]>(`${this.api}/Settlement/Generate`, { year, month, partnerId: partnerId ?? null });
  }

  markPaid(id: number, reference: string, paidAt?: string | null): Observable<Settlement> {
    return this.http.post<Settlement>(`${this.api}/Settlement/${id}/Paid`, { reference, paidAt: paidAt || null });
  }

  exportSettlement(id: number, format: 'csv' | 'pdf', language: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.api}/Settlement/${id}/Export`, { params: toParams({ format, language }), observe: 'response', responseType: 'blob' });
  }
}

/** The partner portal statements (api/partner-portal/statements), scoped to the signed-in partner. */
@Injectable({ providedIn: 'root' })
export class PartnerStatementsService {
  private readonly base = `${environment.apiUrl}/api/partner-portal/statements`;

  constructor(private http: HttpClient) {}

  list(year: number | null, page = 1, pageSize = 24): Observable<PagedResult<Settlement>> {
    return this.http.get<PagedResult<Settlement>>(this.base, { params: toParams({ year, page, pageSize }) });
  }

  detail(id: number): Observable<SettlementDetail> {
    return this.http.get<SettlementDetail>(`${this.base}/${id}`);
  }

  export(id: number, format: 'csv' | 'pdf', language: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/${id}/export`, { params: toParams({ format, language }), observe: 'response', responseType: 'blob' });
  }
}
