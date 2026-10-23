import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AccessService } from 'src/_services/access.service';
import { AuthService } from 'src/_services/auth.service';

/** "/" sends each account to its own space: partner portal, operator dashboard or customer portal. */
@Component({
  selector: 'app-index',
  template: `<p class="vx-empty" role="status">{{ 'common.state.loading' | translate }}</p>`
})
export class IndexComponent implements OnInit {

  constructor(
    private _access: AccessService,
    private _auth: AuthService,
    private _router: Router
  ) { }

  ngOnInit(): void {
    if (!this._auth.loggedIn()) {
      this._router.navigateByUrl('/auth/login', { replaceUrl: true });
      return;
    }
    this._access.load().subscribe({
      next: info => this._router.navigateByUrl(IndexComponent.home(info.role, !!info.partnerId, this._access.canUrl('/dashboard')), { replaceUrl: true }),
      error: () => this._router.navigateByUrl('/auth/login', { replaceUrl: true })
    });
  }

  /** The landing page of an account. */
  static home(role: string, hasPartner: boolean, canUseDashboard: boolean): string {
    if (role === 'Partner' && hasPartner) return '/partner-dashboard';
    return canUseDashboard ? '/dashboard' : '/my-dashboard';
  }
}
