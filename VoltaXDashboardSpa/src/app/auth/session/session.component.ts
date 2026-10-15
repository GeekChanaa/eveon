import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AccessInfo, AccessService } from 'src/_services/access.service';
import { TokenRefreshService } from 'src/_services/token-refresh.service';
import { TokenStorageService } from 'src/_services/token-storage.service';
import { environment } from 'src/environments/environment';

/**
 * Entry point after a sign in on the public site (environment.authUrl).
 *
 * The API set the HttpOnly refresh cookie during that sign in; this page swaps it for an
 * access token, so no token ever travels in the URL, then opens the right dashboard.
 * Query: portal=partner, enroll=1 (2FA enrollment required), returnUrl=/in-app/path.
 */
@Component({
  selector: 'app-session',
  template: `<div class="session-handoff" role="status">{{ message }}</div>`,
  styles: [`.session-handoff { min-height: 100vh; display: flex; align-items: center; justify-content: center; font-size: 15px; }`]
})
export class SessionComponent implements OnInit {

  message = 'Signing you in…';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tokens: TokenStorageService,
    private tokenRefresh: TokenRefreshService,
    private access: AccessService
  ) { }

  ngOnInit() {
    const query = this.route.snapshot.queryParamMap;
    const partner = query.get('portal') === 'partner';
    const enroll = query.get('enroll') === '1';
    const returnUrl = this.safeReturnUrl(query.get('returnUrl'));

    // The cookie is invisible to scripts: assume it exists and let the API decide.
    this.tokens.assumeSession();
    this.tokenRefresh.refresh().subscribe({
      next: () => this.access.load().subscribe({
        next: info => this.router.navigateByUrl(this.destination(info, partner, enroll, returnUrl), { replaceUrl: true }),
        error: () => this.router.navigateByUrl('/access-denied', { replaceUrl: true })
      }),
      error: () => {
        this.tokens.clear();
        this.message = 'Your session could not be opened. Redirecting to sign in…';
        window.location.replace(environment.authUrl + (partner ? '/partner-auth/login' : '/auth/login'));
      }
    });
  }

  private destination(info: AccessInfo, partner: boolean, enroll: boolean, returnUrl: string | null): string {
    const isPartner = info.role === 'Partner' && !!info.partnerId;
    if (enroll) return partner || isPartner ? '/partner-dashboard/security' : '/dashboard/profile?tab=security';
    if (returnUrl) return returnUrl;
    if (isPartner) return '/partner-dashboard';
    if (info.isAdmin || info.permissions.includes('AccessDashboard')) {
      return this.access.canUrl('/dashboard') ? '/dashboard' : '/dashboard/profile';
    }
    return '/my-dashboard';
  }

  /** In-app paths only: "/dashboard/x" yes, "//evil.com" or "https://evil.com" no. */
  private safeReturnUrl(value: string | null): string | null {
    return value && /^\/(?![/\\])/.test(value) ? value : null;
  }
}
