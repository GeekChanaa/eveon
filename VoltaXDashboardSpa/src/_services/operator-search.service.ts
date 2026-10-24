import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import { OperatorSession, OperatorSessionFilter, OperatorTransaction, OperatorTransactionFilter } from 'src/_models/operator';
import { PagedResult } from 'src/_models/portal';

/** Network-wide session and transaction search and exports for the operator dashboard. */
@Injectable({ providedIn: 'root' })
export class OperatorSearchService {
  private readonly sessionsUrl = `${environment.apiUrl}/api/ChargingSession`;
  private readonly transactionsUrl = `${environment.apiUrl}/api/Transaction`;

  constructor(private http: HttpClient) {}

  sessions(filter: OperatorSessionFilter): Observable<PagedResult<OperatorSession>> {
    return this.http.get<PagedResult<OperatorSession>>(`${this.sessionsUrl}/Search`, { params: toParams(filter) });
  }

  exportSessions(filter: OperatorSessionFilter, format: 'csv' | 'pdf'): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.sessionsUrl}/Export`, { params: toParams({ ...rest, format }), observe: 'response', responseType: 'blob' });
  }

  transactions(filter: OperatorTransactionFilter): Observable<PagedResult<OperatorTransaction>> {
    return this.http.get<PagedResult<OperatorTransaction>>(`${this.transactionsUrl}/Search`, { params: toParams(filter) });
  }

  exportTransactions(filter: OperatorTransactionFilter): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.transactionsUrl}/Export`, { params: toParams(rest), observe: 'response', responseType: 'blob' });
  }
}
