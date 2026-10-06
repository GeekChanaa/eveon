import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { Observable, catchError, map, of, switchMap } from 'rxjs';
import { AuthService } from 'src/_services/auth.service';
import { AccessInfo, AccessService } from 'src/_services/access.service';
import { TokenStorageService } from 'src/_services/token-storage.service';

/**
 * Keeps entry points intended for signed-out visitors out of an existing session.
 *
 * Verification and OAuth callback pages deliberately do not use this guard: a new
 * account receives a session before it completes those flows.
 */
@Injectable({ providedIn: 'root' })
export class GuestGuard {
  constructor(
    private auth: AuthService,
    private access: AccessService,
    private router: Router,
    private tokens: TokenStorageService
  ) {}

  canActivate(): Observable<boolean | UrlTree> | boolean | UrlTree {
    if (!this.tokens.isAccessTokenExpired()) {
      return this.redirectAuthenticatedUser();
    }

    if (!this.tokens.hasRefreshToken()) {
      return true;
    }

    // A refresh token represents an active session even when the short-lived
    // access token has expired. Restore it before deciding whether to redirect.
    return this.auth.refreshSession().pipe(
      switchMap(() => this.redirectAuthenticatedUser()),
      catchError(() => of(true))
    );
  }

  private redirectAuthenticatedUser(): Observable<UrlTree> {
    return this.access.load().pipe(
      map(info => this.router.createUrlTree([this.destinationFor(info)])),
      // Preserve the authenticated-route behaviour when a valid session cannot
      // load its account metadata: do not expose guest-only entry screens.
      catchError(() => of(this.router.createUrlTree(['/access-denied'])))
    );
  }

  private destinationFor(info: AccessInfo): string {
    if (info.role === 'Partner' && info.partnerId) return '/partner-dashboard';
    if (info.isAdmin || info.permissions.includes('AccessDashboard')) return '/dashboard';
    return '/my-dashboard';
  }
}
