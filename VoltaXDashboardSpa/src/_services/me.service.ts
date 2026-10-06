import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

// Self-service GDPR endpoints (api/me): everything is scoped to the signed-in account.
@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly baseUrl = environment.apiUrl + '/api/me';

  constructor(private http: HttpClient) {}

  deleteAccount(password: string | null) {
    return this.http.delete<{ scheduledFor: string }>(this.baseUrl, { body: { password } });
  }

  downloadDataExport(token: string) {
    return this.http.get(this.baseUrl + '/data-export/download', { params: { token }, responseType: 'blob' });
  }
}
