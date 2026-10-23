import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import {
  ConnectorPricing, PagedResult, PartnerChargePoint, PartnerConnector, PartnerOverview, PartnerProfile, PartnerSession,
  PartnerSessionFilter, PartnerStation, PartnerStatistics
} from 'src/_models/portal';

/** The partner portal API (api/partner-portal): the signed-in partner's own stations, prices, sessions and revenue. */
@Injectable({ providedIn: 'root' })
export class PartnerPortalService {
  private readonly base = `${environment.apiUrl}/api/partner-portal`;
  private readonly profileSubject = new BehaviorSubject<PartnerProfile | null>(null);

  /** The last loaded profile, shared by the sidebar (logo) and the pages. */
  readonly profile$ = this.profileSubject.asObservable();

  constructor(private http: HttpClient) {}

  /** Absolute URL of a stored file (the API returns paths relative to the static files host). */
  static fileUrl(path: string | null | undefined): string | null {
    if (!path) return null;
    return /^https?:\/\//i.test(path) ? path : environment.apiStaticFilesUrl + path;
  }

  profile(): Observable<PartnerProfile> {
    return this.http.get<PartnerProfile>(`${this.base}/profile`).pipe(tap(profile => this.profileSubject.next(profile)));
  }

  uploadLogo(file: File): Observable<PartnerProfile> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<PartnerProfile>(`${this.base}/profile/logo`, form).pipe(tap(profile => this.profileSubject.next(profile)));
  }

  overview(): Observable<PartnerOverview> { return this.http.get<PartnerOverview>(`${this.base}/overview`); }
  stations(): Observable<PartnerStation[]> { return this.http.get<PartnerStation[]>(`${this.base}/stations`); }

  chargePoints(stationId?: number | null): Observable<PartnerChargePoint[]> {
    return this.http.get<PartnerChargePoint[]>(`${this.base}/charge-points`, { params: toParams({ stationId }) });
  }

  chargePoint(id: number): Observable<PartnerChargePoint> { return this.http.get<PartnerChargePoint>(`${this.base}/charge-points/${id}`); }

  updatePricing(connectorId: number, pricing: ConnectorPricing): Observable<PartnerConnector> {
    return this.http.put<PartnerConnector>(`${this.base}/connectors/${connectorId}/pricing`, pricing);
  }

  sessions(filter: PartnerSessionFilter): Observable<PagedResult<PartnerSession>> {
    return this.http.get<PagedResult<PartnerSession>>(`${this.base}/sessions`, { params: toParams(filter) });
  }

  statistics(): Observable<PartnerStatistics> { return this.http.get<PartnerStatistics>(`${this.base}/statistics`); }

  statement(filter: PartnerSessionFilter, format: 'csv' | 'pdf'): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.base}/statement`, { params: toParams({ ...rest, format }), observe: 'response', responseType: 'blob' });
  }
}
