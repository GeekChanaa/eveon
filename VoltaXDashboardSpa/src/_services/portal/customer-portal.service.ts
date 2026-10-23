import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import {
  MyCard, MyOrder, MyOverview, MyProfile, MySession, MySessionDetail, MySessionFilter, MyWalletMovement, PagedResult
} from 'src/_models/portal';

/** The customer portal API (api/my): the signed-in customer's own data, read only. */
@Injectable({ providedIn: 'root' })
export class CustomerPortalService {
  private readonly base = `${environment.apiUrl}/api/my`;

  constructor(private http: HttpClient) {}

  overview(): Observable<MyOverview> { return this.http.get<MyOverview>(`${this.base}/overview`); }
  profile(): Observable<MyProfile> { return this.http.get<MyProfile>(`${this.base}/profile`); }
  cards(): Observable<MyCard[]> { return this.http.get<MyCard[]>(`${this.base}/cards`); }

  sessions(filter: MySessionFilter): Observable<PagedResult<MySession>> {
    return this.http.get<PagedResult<MySession>>(`${this.base}/sessions`, { params: toParams(filter) });
  }

  session(id: number): Observable<MySessionDetail> { return this.http.get<MySessionDetail>(`${this.base}/sessions/${id}`); }

  sessionInvoice(id: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/sessions/${id}/invoice`, { observe: 'response', responseType: 'blob' });
  }

  exportSessions(filter: MySessionFilter, format: 'csv' | 'pdf'): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.base}/sessions/export`, { params: toParams({ ...rest, format }), observe: 'response', responseType: 'blob' });
  }

  wallet(page = 1, pageSize = 10): Observable<PagedResult<MyWalletMovement>> {
    return this.http.get<PagedResult<MyWalletMovement>>(`${this.base}/wallet`, { params: toParams({ page, pageSize }) });
  }

  orders(page = 1, pageSize = 10): Observable<PagedResult<MyOrder>> {
    return this.http.get<PagedResult<MyOrder>>(`${this.base}/orders`, { params: toParams({ page, pageSize }) });
  }

  orderInvoice(id: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/orders/${id}/invoice`, { observe: 'response', responseType: 'blob' });
  }
}
