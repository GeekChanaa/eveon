import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import {
  MyActiveSession, MyCard, MyOrder, MyOverview, MyProfile, MyProfileUpdate, MySession, MySessionDetail, MySessionFilter, MyWalletMovement, PagedResult
} from 'src/_models/portal';

/** The customer portal API (api/my): the signed-in customer's own data, read only but for the optional profile details. */
@Injectable({ providedIn: 'root' })
export class CustomerPortalService {
  private readonly base = `${environment.apiUrl}/api/my`;

  constructor(private http: HttpClient) {}

  overview(): Observable<MyOverview> { return this.http.get<MyOverview>(`${this.base}/overview`); }
  profile(): Observable<MyProfile> { return this.http.get<MyProfile>(`${this.base}/profile`); }
  updateProfile(update: MyProfileUpdate): Observable<void> { return this.http.put<void>(`${this.base}/profile`, update); }
  cards(): Observable<MyCard[]> { return this.http.get<MyCard[]>(`${this.base}/cards`); }

  sessions(filter: MySessionFilter): Observable<PagedResult<MySession>> {
    return this.http.get<PagedResult<MySession>>(`${this.base}/sessions`, { params: toParams(filter) });
  }

  /** The running session, or null (204) when nothing is charging. */
  activeSession(): Observable<MyActiveSession | null> {
    return this.http.get<MyActiveSession>(`${this.base}/sessions/active`, { observe: 'response' }).pipe(map(r => r.status === 204 ? null : r.body));
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
