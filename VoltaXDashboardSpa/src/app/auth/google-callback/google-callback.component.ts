import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';
import { AccessService } from 'src/_services/access.service';

/**
 * Landing page of the Google redirect flow. The API sends the browser here with
 * either a VoltaX token, a "linked" flag or an error message.
 */
@Component({
  selector: 'app-google-callback',
  templateUrl: './google-callback.component.html',
  styleUrls: ['./google-callback.component.sass']
})
export class GoogleCallbackComponent implements OnInit {

  errorMessage: string = "";

  constructor(
    private _route: ActivatedRoute,
    private _router: Router,
    private _authService: AuthService,
    private access: AccessService
  ) { }

  ngOnInit() {
    {
      const fragment = new URLSearchParams(this._route.snapshot.fragment || '');
      const params: Record<string, any> = { ...this._route.snapshot.queryParams };
      fragment.forEach((value, key) => params[key] = value);
      window.history.replaceState(window.history.state, '', window.location.pathname);
      const error = params['error'];
      const token = params['token'];
      const twoFactorToken = params['twoFactorToken'];
      const linked = params['linked'];
      const returnUrl = params['returnUrl'];

      if (error) {
        this.errorMessage = error;
        return;
      }

      // Coming back from "link my Google account" on the profile page
      if (linked === 'true') {
        this._router.navigateByUrl(returnUrl || '/dashboard/profile');
        return;
      }

      // Google proved the identity, the account still asks for its second factor.
      if (twoFactorToken) {
        this._router.navigateByUrl('/auth/login', { state: { twoFactorToken } });
        return;
      }

      if (!token) {
        this.errorMessage = "Google sign in did not return a token, please try again.";
        return;
      }

      this._authService.completeExternalLogin(token);

      if (params['enrollTwoFactor'] === 'true') {
        this._router.navigateByUrl('/dashboard/profile?tab=security');
        return;
      }

      if (returnUrl) {
        this._router.navigateByUrl(returnUrl);
        return;
      }

      this.access.load().subscribe({
        next: () => this._router.navigateByUrl(this.access.canUrl('/dashboard') ? '/dashboard' :
          this.access.can('AccessDashboard') ? '/dashboard/profile' : '/my-dashboard'),
        error: () => this._router.navigateByUrl('/access-denied')
      });
    }
  }

  backToLogin() {
    this._router.navigateByUrl('/auth/login');
  }

}
