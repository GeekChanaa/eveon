import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

export interface AuditLogEntry {
  id: number;
  occurredAt: string;
  userID?: number;
  userEmail?: string;
  role?: string;
  action: string;
  entityType?: string;
  entityID?: string;
  changesJson?: string;
  ipAddress?: string;
  correlationID?: string;
}

export interface AuditLogFilters {
  userId?: number | null;
  user?: string;
  entityType?: string;
  entityId?: string;
  action?: string;
  from?: string;
  to?: string;
}

export interface AuditLogPage { items: AuditLogEntry[]; totalCount: number; page: number; pageSize: number; }

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly baseUrl = environment.apiUrl + '/api/audit-logs';

  constructor(private http: HttpClient) {}

  list(page: number, pageSize: number, filters: AuditLogFilters) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== null && value !== undefined && `${value}`.trim() !== '') params = params.set(key, `${value}`.trim());
    });
    return this.http.get<AuditLogPage>(this.baseUrl, { params });
  }

  entityTypes() {
    return this.http.get<string[]>(this.baseUrl + '/entity-types');
  }
}
