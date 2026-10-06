import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable, InjectionToken } from '@angular/core';
import { defer, firstValueFrom, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { TokenStorageService } from './token-storage.service';

export const REFRESH_LOCKS = new InjectionToken<LockManager | null>('Refresh locks', {
  providedIn: 'root',
  factory: () => typeof navigator !== 'undefined' ? navigator.locks ?? null : null
});

/** Tells the API to keep the refresh token in its HttpOnly cookie and out of the JSON body. */
export const COOKIE_TRANSPORT_HEADERS = new HttpHeaders({ 'X-Token-Transport': 'cookie' });

/**
 * Exchanges the refresh cookie for a new access token.
 *
 * Deliberately free of any dependency on AuthService: the HTTP interceptor calls
 * this, and going through AuthService would close a DI cycle
 * (interceptor -> AuthService -> HttpClient -> interceptor).
 *
 * Concurrent callers share one in-flight request, and tabs are serialized with a Web
 * Lock: every tab sends the same cookie and the API spends it on use, so two parallel
 * refreshes would make the second one look like a replay and kill the whole session.
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
   * Resolves with the new access token. Fails when no session is known or the API
   * rejects the cookie — the caller is then expected to send the user to the login page.
   */
  refresh(): Observable<string> {
    if (this.inFlight != null) {
      return this.inFlight;
    }

    if (!this._tokenStorage.hasRefreshToken()) {
      return throwError(() => new Error('No session to refresh'));
    }

    const generation = this._tokenStorage.generation;
    const exchange = () => this.exchange(generation);
    const request = this.locks
      ? defer(() => this.locks!.request('voltax-refresh', () => {
          if (!this._tokenStorage.hasRefreshToken()) {
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

  private exchange(generation: number): Observable<string> {
    return this._http.post<any>(this.baseUrl + "Refresh", {}, { withCredentials: true, headers: COOKIE_TRANSPORT_HEADERS }).pipe(
      tap((result) => {
        if (!result?.token) {
          throw new Error('Invalid refresh response');
        }
        // A response from an old session must never resurrect it after logout/login.
        if (this._tokenStorage.generation !== generation) {
          throw new Error('Session changed during refresh');
        }
        this._tokenStorage.store(result);
      }),
      map((result) => result.token as string),
      catchError((error) => {
        // A refused cookie is unusable from here on: forget the session so the app stops
        // retrying and asks for a real login instead.
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)
            && this._tokenStorage.generation === generation) {
          this._tokenStorage.clear();
        }
        return throwError(() => error);
      })
    );
  }

  /** Ends the session server side. Errors are swallowed: a sign out must always succeed locally. */
  revoke(): Observable<any> {
    this._tokenStorage.clear();
    return this._http.post(this.baseUrl + "Logout", {}, { withCredentials: true, headers: COOKIE_TRANSPORT_HEADERS }).pipe(
      catchError(() => of(null))
    );
  }
}
