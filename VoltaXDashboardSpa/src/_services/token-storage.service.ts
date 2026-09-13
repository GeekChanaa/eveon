import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';

/**
 * Single owner of the two tokens a session is made of.
 *
 * The access token is short lived (an hour by default) and goes out on every
 * request; the refresh token is long lived, is only ever sent to /api/auth/refresh
 * and is rotated each time it is spent — so whatever comes back has to replace what
 * was stored, otherwise the next refresh is rejected as a replay.
 */
@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  static readonly ACCESS_TOKEN_KEY = 'token';
  static readonly REFRESH_TOKEN_KEY = 'refreshToken';
  static readonly ACCESS_EXPIRES_KEY = 'tokenExpiresAt';

  private jwtHelper = new JwtHelperService();

  get accessToken(): string | null {
    return localStorage.getItem(TokenStorageService.ACCESS_TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(TokenStorageService.REFRESH_TOKEN_KEY);
  }

  /** Stores a login / refresh result. Missing fields are left untouched rather than cleared. */
  store(result: any): void {
    if (result == null) return;

    if (result.token) {
      localStorage.setItem(TokenStorageService.ACCESS_TOKEN_KEY, result.token);
    }

    if (result.refreshToken) {
      localStorage.setItem(TokenStorageService.REFRESH_TOKEN_KEY, result.refreshToken);
    }

    // The API sends the expiry so the client can refresh ahead of a 401. When it is
    // absent (older endpoints) the JWT itself still carries one.
    const expiresAt = result.accessTokenExpiresAt ?? this.expiryFromJwt(result.token);
    if (expiresAt) {
      localStorage.setItem(TokenStorageService.ACCESS_EXPIRES_KEY, new Date(expiresAt).toISOString());
    }
  }

  storeAccessToken(token: string): void {
    this.store({ token: token });
  }

  clear(): void {
    localStorage.removeItem(TokenStorageService.ACCESS_TOKEN_KEY);
    localStorage.removeItem(TokenStorageService.REFRESH_TOKEN_KEY);
    localStorage.removeItem(TokenStorageService.ACCESS_EXPIRES_KEY);
  }

  hasRefreshToken(): boolean {
    const token = this.refreshToken;
    return token != null && token !== '';
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
}
