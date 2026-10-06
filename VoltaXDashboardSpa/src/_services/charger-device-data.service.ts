import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

export interface Paged<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

export interface ChargerEvent {
  id: number; chargePointID: string; eventId: number; timestamp: string; trigger: string; actualValue?: string;
  techCode?: string; techInfo?: string; cleared?: boolean; cause?: number; transactionId?: string;
  componentName: string; componentInstance?: string; variableName: string; variableInstance?: string;
  evseId?: number; connectorId?: number; variableMonitoringId?: number; eventNotificationType: string;
  severity?: number; receivedAt?: string;
}

export interface ChargerEventFilters {
  chargePointId?: string; from?: string; to?: string; maxSeverity?: number | null; alarmsOnly?: boolean;
  component?: string; variable?: string; search?: string;
}

export interface VariableMonitor {
  id: number; componentName: string; componentInstance?: string; variableName: string; variableInstance?: string;
  evseId?: number; connectorId?: number; monitoringId: number; type: string; value: number; severity: number;
  transaction: boolean; reportedAt: string;
}

export interface DisplayMessageSnapshot {
  id: number; messageId: number; priority?: string; state?: string; startDateTime?: string; endDateTime?: string;
  transactionId?: string; content?: string; format?: string; language?: string;
  displayComponentName?: string; displayComponentInstance?: string; displayEvseId?: number;
}

export interface LatestDisplayMessages { requestId?: number; receivedAt?: string; messages: DisplayMessageSnapshot[]; }

export interface CustomerInformationReport {
  id: number; chargePointID: string; requestId: number; report: boolean; clear: boolean; customerIdentifier?: string;
  idToken?: string; commandStatus?: string; requestedAt: string; partsReceived: number; complete: boolean;
  completedAt?: string; data?: string;
}

export interface UploadedChargerLog { id: number; fileName: string; sizeBytes: number; sha256: string; contentType?: string; uploadedAt: string; }
export interface LogUploadTicket {
  id: number; requestId: number; purpose: string; createdAt: string; expiresAt: string; usedAt?: string;
  status?: string; statusAt?: string; announcedFileName?: string; files: UploadedChargerLog[];
}

/** OCPP 2.0.1 device data: events, monitors, display messages, customer information and uploaded logs. */
@Injectable({ providedIn: 'root' })
export class ChargerDeviceDataService {
  private readonly api = environment.apiUrl + '/api/';
  private readonly ocpp = environment.apiUrl + '/ocpp/';

  constructor(private http: HttpClient) {}

  events(page: number, pageSize: number, filters: ChargerEventFilters) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== false && `${value}`.trim() !== '') params = params.set(key, `${value}`.trim());
    });
    return this.http.get<Paged<ChargerEvent>>(this.api + 'ChargerEvent', { params });
  }

  monitors(chargePointId: string) {
    return this.http.get<VariableMonitor[]>(this.api + 'ChargerEvent/monitors/' + encodeURIComponent(chargePointId));
  }

  latestDisplayMessages(chargePointId: string) {
    return this.http.get<LatestDisplayMessages>(this.api + 'ChargerDisplayMessage/' + encodeURIComponent(chargePointId));
  }

  refreshDisplayMessages(chargePointId: string) {
    return this.http.post<any>(this.ocpp + 'DisplayMessages/Refresh/' + encodeURIComponent(chargePointId), {});
  }

  setDisplayMessage(chargePointId: string, request: any) {
    return this.http.post<any>(this.ocpp + 'DisplayMessages/Set/' + encodeURIComponent(chargePointId), request);
  }

  clearDisplayMessage(chargePointId: string, id: number) {
    return this.http.post<any>(this.ocpp + 'DisplayMessages/Clear/' + encodeURIComponent(chargePointId), { id });
  }

  customerInformation(chargePointId: string, page: number, pageSize: number) {
    const params = new HttpParams().set('chargePointId', chargePointId).set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<CustomerInformationReport>>(this.api + 'CustomerInformationReport', { params });
  }

  requestCustomerInformation(chargePointId: string, request: any) {
    return this.http.post<any>(this.ocpp + 'Reporting/CustomerInformation/' + encodeURIComponent(chargePointId), request);
  }

  logs(chargePointId: string) {
    return this.http.get<LogUploadTicket[]>(this.api + 'logs/' + encodeURIComponent(chargePointId));
  }

  requestLog(chargePointId: string, logType: 'DiagnosticsLog' | 'SecurityLog') {
    // No remoteLocation: the API hands the charger a one-time upload URL of this platform.
    return this.http.post<any>(this.ocpp + 'Reporting/GetLog/' + encodeURIComponent(chargePointId), { logType, requestId: newRequestId(), log: {} });
  }

  downloadLog(id: number) {
    return this.http.get(this.api + 'logs/file/' + id, { responseType: 'blob' });
  }
}

/** A requestId for a CSMS command (OCPP integer). */
export function newRequestId(): number {
  return Math.floor(Math.random() * 2_000_000_000) + 1;
}
