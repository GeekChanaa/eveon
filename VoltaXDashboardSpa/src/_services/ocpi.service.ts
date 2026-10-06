import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

export interface OcpiSettings { enabled: boolean; countryCode: string; partyId: string; versionsUrl: string; }

export interface OcpiEndpoint { identifier: string; role: string; url: string; }

export interface OcpiPartner {
  id: number;
  name: string;
  countryCode?: string;
  partyId?: string;
  role?: string;
  status: 'Pending' | 'Registered' | 'Suspended' | 'Unregistered';
  versionsUrl?: string;
  version?: string;
  lastError?: string;
  registeredAt?: string;
  createdAt: string;
  updatedAt: string;
  awaitingTokenA: boolean;
  endpoints: OcpiEndpoint[];
  failedMessages: number;
  pendingMessages: number;
  tokens: number;
}

export interface OcpiTokenA { id: number; tokenA: string; versionsUrl: string; }

export interface OcpiOutboxMessage {
  id: number;
  ocpiPartyID: number;
  partyName: string;
  module: string;
  method: string;
  url: string;
  attempts: number;
  lastError?: string;
  createdAt: string;
  nextAttemptAt: string;
  sentAt?: string;
}

export interface OcpiOutboxPage { items: OcpiOutboxMessage[]; totalCount: number; page: number; pageSize: number; }

@Injectable({ providedIn: 'root' })
export class OcpiService {
  private readonly baseUrl = environment.apiUrl + '/api/ocpi';

  constructor(private http: HttpClient) {}

  settings() { return this.http.get<OcpiSettings>(`${this.baseUrl}/settings`); }

  partners() { return this.http.get<OcpiPartner[]>(`${this.baseUrl}/partners`); }

  createTokenA(name: string) { return this.http.post<OcpiTokenA>(`${this.baseUrl}/partners/token-a`, { name }); }

  register(name: string, versionsUrl: string, tokenA: string) {
    return this.http.post<{ id: number; countryCode: string; partyId: string; status: string }>(`${this.baseUrl}/partners/register`, { name, versionsUrl, tokenA });
  }

  suspend(id: number) { return this.http.post<void>(`${this.baseUrl}/partners/${id}/suspend`, {}); }

  resume(id: number) { return this.http.post<void>(`${this.baseUrl}/partners/${id}/resume`, {}); }

  refreshEndpoints(id: number) { return this.http.post<void>(`${this.baseUrl}/partners/${id}/refresh-endpoints`, {}); }

  unregister(id: number) { return this.http.delete<void>(`${this.baseUrl}/partners/${id}`); }

  outbox(page: number, pageSize: number, status = 'Failed', partyId?: number | null) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize).set('status', status);
    if (partyId) params = params.set('partyId', partyId);
    return this.http.get<OcpiOutboxPage>(`${this.baseUrl}/outbox`, { params });
  }

  retry(id: number) { return this.http.post<void>(`${this.baseUrl}/outbox/${id}/retry`, {}); }
}
