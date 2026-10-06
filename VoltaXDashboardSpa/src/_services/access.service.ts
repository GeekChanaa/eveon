import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, tap, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';
import { TokenStorageService } from './token-storage.service';

export interface AccessInfo { userId: number; roleId: number; role: string; isAdmin: boolean; permissions: string[]; firstName: string; lastName: string; email: string; imageUrl?: string; partnerId?: number; }
export const DASHBOARD_RESOURCES: Record<string, string> = {
  'charging-stations': 'ChargingStations', 'charging-points': 'ChargePoints', 'connector-realtime': 'ChargePoints',
  'charging-cards': 'ChargingCards', 'notices': 'Notices', 'reports': 'Reports', 'system-reports': 'SystemReports',
  'user-info-download-requests': 'UserInfoDownloadRequests', 'charging-sessions': 'ChargingSessions',
  'transactions': 'Transactions', 'recharge-orders': 'RechargeOrders', 'users': 'Users', 'partners': 'Partners',
  'comments': 'Comments', 'ocpp-configuration': 'ChargePointConfiguration', 'charge-point-configurations': 'ChargePointConfiguration', 'ocpp-local-list': 'OcppLocalLists',
  'global-configurations': 'GlobalConfigurations', 'charging-profile': 'ChargePointConfiguration',
  'charging-strategies': 'ChargePointConfiguration', 'station-load-balance': 'ChargePointConfiguration',
  'home-charger-bind-list': 'ChargePoints', 'alarm-management': 'Reports', 'charger-events': 'ChargePoints'
};
export function routePermission(url: string): string | null {
  const parts = url.split(/[?#]/)[0].split('/').filter(Boolean);
  if (parts[0] !== 'dashboard') return null;
  const feature = parts[1] || '';
  if (!feature) return 'ViewDashboard';
  if (feature === 'profile') return 'AccessDashboard';
  if (feature === 'statistics') return 'ViewStatisticsPage';
  if (feature === 'documentation') return 'ViewDocumentation';
  if (feature === 'roles' || feature === 'permissions') return '__admin';
  if (feature === 'connector-realtime' && parts[3] === 'charging-session') return 'ViewChargingSessions';
  const resource = DASHBOARD_RESOURCES[feature];
  if (!resource) return '__admin';
  const operation = parts.slice(2).some(p => p === 'create' || p === 'add') ? 'Create' : parts.slice(2).includes('edit') ? 'Edit' : 'View';
  return operation + resource;
}
@Injectable({ providedIn: 'root' })
export class AccessService {
  private token: string | null = null;
  readonly state = new BehaviorSubject<AccessInfo | null>(null);
  readonly baseUrl = environment.apiUrl + '/api/access';
  constructor(private http: HttpClient, private tokens: TokenStorageService) {}
  get current(): AccessInfo | null { return this.tokens.accessToken === this.token ? this.state.value : null; }
  get isAdmin(): boolean { return this.current?.isAdmin === true; }
  can(permission: string): boolean { return !!this.current && (this.isAdmin || this.current.permissions.includes(permission)); }
  canUrl(url: string): boolean { const required = routePermission(url); return required !== null && this.can('AccessDashboard') && this.can(required); }
  load() {
    return this.http.get<AccessInfo>(this.baseUrl + '/me').pipe(tap(info => { this.token = this.tokens.accessToken; this.state.next(info); }),
      catchError(error => { this.state.next(null); return throwError(() => error); }));
  }
  profile() { return this.http.get<any>(this.baseUrl + '/profile'); }
  updateProfile(model: any) { return this.http.put(this.baseUrl + '/profile', { firstName: model.firstName, lastName: model.lastName, birthday: model.birthday }); }
  assignRole(userId: number, roleId: number) { return this.http.put<any>(this.baseUrl + '/users/' + userId + '/role', { roleId }); }
}
