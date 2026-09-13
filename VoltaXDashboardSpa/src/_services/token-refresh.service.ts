import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Inject, Injectable, InjectionToken } from '@angular/core';
import { defer, firstValueFrom, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { TokenStorageService } from './token-storage.service';

export const REFRESH_LOCKS = new InjectionToken<LockManager | null>('Refresh locks', {
  providedIn: 'root',
  factory: () => typeof navigator !== 'undefined' ? navigator.locks ?? null : null
});

/**
 * Exchanges the refresh token for a fresh pair.
 *
 * Deliberately free of any dependency on AuthService: the HTTP interceptor calls
 * this, and going through AuthService would close a DI cycle
 * (interceptor -> AuthService -> HttpClient -> interceptor).
 *
 * Concurrent callers share one in-flight request. That matters because the API spends
 * the refresh token it is handed: two parallel refreshes would make the second one look
 * like a replay and kill the whole session.
 */
@Injectable({
  providedIn: 'root'
})
export class TokenRefreshService {

  private baseUrl = environment.apiUrl + "/api/auth/";

  /** The refresh currently in flight, if any. */
  private inFlight: Observable<string> | null = null;

  constructor(
    private _http: HttpClient,
    private _tokenStorage: TokenStorageService,
    @Inject(REFRESH_LOCKS) private locks: LockManager | null
  ) { }

  get isRefreshing(): boolean {
    return this.inFlight != null;
  }

  /**
   * Resolves with the new access token. Fails when there is no refresh token to spend
   * or the API rejects it — the caller is then expected to send the user to the login page.
   */
  refresh(): Observable<string> {
    if (this.inFlight != null) {
      return this.inFlight;
    }

    const refreshToken = this._tokenStorage.refreshToken;
    if (refreshToken == null || refreshToken === '') {
      return throwError(() => new Error('No refresh token available'));
    }

    const exchange = () => this.exchange(refreshToken);
    // Tabs share localStorage, so serialize rotations across tabs where Web Locks exist.
    const request = this.locks
      ? defer(() => this.locks!.request('voltax-refresh', () => {
          if (this._tokenStorage.refreshToken !== refreshToken) {
            const token = this._tokenStorage.accessToken;
            if (token && this._tokenStorage.hasRefreshToken()) return Promise.resolve(token);
            return Promise.reject(new Error('Session ended while waiting to refresh'));
          }
          return firstValueFrom(exchange());
        }))
      : defer(exchange);

    this.inFlight = request.pipe(
      finalize(() => this.inFlight = null),
      shareReplay(1)
    );
    return this.inFlight;
  }

  private exchange(refreshToken: string): Observable<string> {
    return this._http.post<any>(this.baseUrl + "Refresh", { refreshToken }).pipe(
      tap((result) => {
        if (!result?.token || !result?.refreshToken) {
          throw new Error('Invalid refresh response');
        }
        // A response from an old session must never resurrect it after logout/login.
        if (this._tokenStorage.refreshToken !== refreshToken) {
          this._http.post(this.baseUrl + 'Logout', { refreshToken: result.refreshToken }).subscribe({ error: () => {} });
          throw new Error('Session changed during refresh');
        }
        this._tokenStorage.store(result);
      }),
      map((result) => result.token as string),
      catchError((error) => {
        // A refused refresh token is unusable from here on: drop it so the app stops
        // retrying and asks for a real login instead.
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)
            && this._tokenStorage.refreshToken === refreshToken) {
          this._tokenStorage.clear();
        }
        return throwError(() => error);
      })
    );
  }

  /** Ends the session server side. Errors are swallowed: a sign out must always succeed locally. */
  revoke(): Observable<any> {
    const refreshToken = this._tokenStorage.refreshToken;
    this._tokenStorage.clear();
    if (refreshToken == null || refreshToken === '') {
      return of(null);
    }

    return this._http.post(this.baseUrl + "Logout", { refreshToken: refreshToken }).pipe(
      catchError(() => of(null))
    );
  }
}
