import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment';
import { toParams } from 'src/_helpers/download';
import {
  ConnectorPricing, PagedResult, PartnerChargePoint, PartnerConnector, PartnerOverview, PartnerProfile, PartnerResetType, PartnerSession,
  PartnerSessionFilter, PartnerStation, PartnerStationDetail, PartnerStatistics, UpdatePartnerStation
} from 'src/_models/portal';
import { CommandAnswer } from 'src/app/shared/portal-ui/remote-command';

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

  // ------------------------------------------------------------------ the partner's stations

  station(id: number): Observable<PartnerStationDetail> { return this.http.get<PartnerStationDetail>(`${this.base}/stations/${id}`); }

  updateStation(id: number, station: UpdatePartnerStation): Observable<PartnerStationDetail> {
    return this.http.put<PartnerStationDetail>(`${this.base}/stations/${id}`, station);
  }

  addStationPhotos(id: number, files: File[]): Observable<PartnerStationDetail> {
    const form = new FormData();
    files.forEach(file => form.append('photos', file, file.name));
    return this.http.post<PartnerStationDetail>(`${this.base}/stations/${id}/photos`, form);
  }

  removeStationPhoto(id: number, photoId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/stations/${id}/photos/${photoId}`);
  }

  // ------------------------------------------------------------------ remote actions (answers: see CommandAnswer)

  /** Soft: restart once the sessions are over; Hard: restart now (running sessions stop). */
  reset(chargePointId: number, type: PartnerResetType): Observable<CommandAnswer> {
    return this.http.post<CommandAnswer>(`${this.base}/charge-points/${chargePointId}/reset`, { type });
  }

  refreshStatus(chargePointId: number): Observable<CommandAnswer> {
    return this.http.post<CommandAnswer>(`${this.base}/charge-points/${chargePointId}/refresh-status`, {});
  }

  unlock(connectorId: number): Observable<CommandAnswer> {
    return this.http.post<CommandAnswer>(`${this.base}/connectors/${connectorId}/unlock`, {});
  }

  stopSession(sessionId: number): Observable<CommandAnswer> {
    return this.http.post<CommandAnswer>(`${this.base}/sessions/${sessionId}/stop`, {});
  }

  statement(filter: PartnerSessionFilter, format: 'csv' | 'pdf'): Observable<HttpResponse<Blob>> {
    const { page, pageSize, ...rest } = filter;
    return this.http.get(`${this.base}/statement`, { params: toParams({ ...rest, format }), observe: 'response', responseType: 'blob' });
  }
}
