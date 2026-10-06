import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';

/**
 * Single owner of the session tokens.
 *
 * The access token is short lived and lives in memory only, so a script injected in the
 * page cannot read it back from storage. The refresh token never reaches JavaScript: the
 * API keeps it in an HttpOnly cookie scoped to /api/auth and rotates it on every refresh.
 * After a reload the app simply calls /api/auth/refresh (withCredentials) to get a new
 * access token.
 *
 * localStorage only holds a non secret "a session probably exists" hint, so guards know
 * whether trying a refresh is worth it.
 */
@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  static readonly SESSION_HINT_KEY = 'hasSession';
  // Keys written by older versions, which kept both tokens in localStorage.
  private static readonly LEGACY_KEYS = ['token', 'refreshToken', 'tokenExpiresAt'];

  private jwtHelper = new JwtHelperService();
  private _accessToken: string | null = null;
  private _generation = 0;

  constructor() {
    TokenStorageService.LEGACY_KEYS.forEach(key => this.safeRemove(key));
  }

  get accessToken(): string | null {
    return this._accessToken;
  }

  /** Changes on every sign in / sign out, so a late refresh response can tell it is stale. */
  get generation(): number {
    return this._generation;
  }

  /** Stores a login / refresh result. Only the access token is kept; refresh tokens stay in the cookie. */
  store(result: any): void {
    if (result?.token) {
      this._accessToken = result.token;
      this.safeSet(TokenStorageService.SESSION_HINT_KEY, '1');
    }
  }

  /** Starts a new session (login), invalidating any refresh still in flight for the old one. */
  startSession(result: any): void {
    this.clear();
    this.store(result);
  }

  storeAccessToken(token: string): void {
    this.store({ token: token });
  }

  clear(): void {
    this._accessToken = null;
    this._generation++;
    this.safeRemove(TokenStorageService.SESSION_HINT_KEY);
  }

  /** True when a refresh cookie is believed to exist (the cookie itself is unreadable by design). */
  hasRefreshToken(): boolean {
    try {
      return localStorage.getItem(TokenStorageService.SESSION_HINT_KEY) === '1';
    } catch {
      return this._accessToken != null;
    }
  }

  isAccessTokenExpired(): boolean {
    const token = this.accessToken;
    if (token == null || token === '') return true;

    try {
      return this.jwtHelper.isTokenExpired(token);
    } catch {
      return true;
    }
  }

  /**
   * True once the access token is inside its last minute of life. Used to refresh
   * proactively instead of paying for a round trip that is going to 401.
   */
  isAccessTokenAboutToExpire(skewSeconds: number = 60): boolean {
    const token = this.accessToken;
    if (token == null || token === '') return true;

    const expiry = this.expiryFromJwt(token);
    if (expiry == null) return false;

    return expiry.getTime() - Date.now() <= skewSeconds * 1000;
  }

  private expiryFromJwt(token: string | null | undefined): Date | null {
    if (token == null || token === '') return null;

    try {
      return this.jwtHelper.getTokenExpirationDate(token);
    } catch {
      return null;
    }
  }

  private safeSet(key: string, value: string) {
    try { localStorage.setItem(key, value); } catch { }
  }

  private safeRemove(key: string) {
    try { localStorage.removeItem(key); } catch { }
  }
}
