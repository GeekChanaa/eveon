import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Alarm, AlarmAssignee, AlarmChange, AlarmFilter, AlarmPage, AlarmSummary } from 'src/_models/alarm';
import { RealtimeHubService, chargerGroup } from './realtime-hub.service';

/** Hub group of the alarm feed (AlarmNotifier.HubGroup on the API). */
export const ALARM_FEED_GROUP = '#alarms';

/** Charge point alarms: list, summary, export, operator actions and the live feed. */
@Injectable({ providedIn: 'root' })
export class AlarmService {
  private readonly url = `${environment.apiUrl}/api/Alarm`;

  constructor(private http: HttpClient, private realtime: RealtimeHubService) {}

  list(filter: AlarmFilter): Observable<AlarmPage> {
    return this.http.get<AlarmPage>(this.url, { params: AlarmService.params(filter) });
  }

  summary(): Observable<AlarmSummary> {
    return this.http.get<AlarmSummary>(`${this.url}/summary`);
  }

  get(id: number): Observable<Alarm> {
    return this.http.get<Alarm>(`${this.url}/${id}`);
  }

  assignees(): Observable<AlarmAssignee[]> {
    return this.http.get<AlarmAssignee[]>(`${this.url}/assignees`);
  }

  acknowledge(id: number): Observable<Alarm> {
    return this.http.post<Alarm>(`${this.url}/${id}/acknowledge`, {});
  }

  assign(id: number, assignedToID: number | null): Observable<Alarm> {
    return this.http.post<Alarm>(`${this.url}/${id}/assign`, { assignedToID });
  }

  resolve(id: number, comment: string): Observable<Alarm> {
    return this.http.post<Alarm>(`${this.url}/${id}/resolve`, { comment });
  }

  /** The filtered alarms as a CSV file (every page). */
  export(filter: AlarmFilter): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.url}/export`, { params: AlarmService.params(filter, false), observe: 'response', responseType: 'blob' });
  }

  /** Every alarm change, pushed while subscribed. */
  changes(): Observable<AlarmChange> {
    return this.realtime.on<AlarmChange>('charger', 'AlarmChanged', chargerGroup(ALARM_FEED_GROUP));
  }

  /** Fires when the live feed reconnected (changes may have been missed). */
  reconnected(): Observable<void> {
    return this.realtime.reconnected('charger');
  }

  /** Query string of a filter: empty values left out, dates as whole days in local time. */
  static params(filter: AlarmFilter, paging = true): HttpParams {
    let params = new HttpParams().set('status', filter.status || 'active');
    const set = (key: string, value: unknown) => {
      if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
    };
    set('severity', filter.severity);
    set('type', filter.type);
    set('chargingStationID', filter.chargingStationID);
    set('chargePointID', filter.chargePointID);
    if (filter.assigned === 'me') set('assignedToMe', true);
    if (filter.assigned === 'none') set('unassigned', true);
    if (filter.from) set('from', new Date(filter.from + 'T00:00:00').toISOString());
    if (filter.to) set('to', new Date(filter.to + 'T23:59:59.999').toISOString());
    set('search', filter.search?.trim());
    set('sort', filter.sort);
    if (paging) {
      set('page', filter.page);
      set('pageSize', filter.pageSize);
    }
    return params;
  }
}
