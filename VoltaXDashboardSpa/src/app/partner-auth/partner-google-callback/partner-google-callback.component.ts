import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PartnerAuthService } from 'src/_services/partner-auth.service';

/**
 * Landing page of the Google redirect flow for the partner portal.
 */
@Component({
  selector: 'app-partner-google-callback',
  templateUrl: './partner-google-callback.component.html',
  styleUrls: ['./partner-google-callback.component.sass']
})
export class PartnerGoogleCallbackComponent implements OnInit {

  errorMessage: string = "";

  constructor(
    private _route: ActivatedRoute,
    private _router: Router,
    private _partnerAuthService: PartnerAuthService
  ) { }

  ngOnInit() {
    {
      const fragment = new URLSearchParams(this._route.snapshot.fragment || '');
      const params: Record<string, any> = { ...this._route.snapshot.queryParams };
      fragment.forEach((value, key) => params[key] = value);
      window.history.replaceState(window.history.state, '', window.location.pathname);
      const error = params['error'];
      const token = params['token'];
      const refreshToken = params['refreshToken'];
      const returnUrl = params['returnUrl'];

      if (error) {
        this.errorMessage = error == 'not a partner account'
          ? "This Google account is not linked to a partner. Use the customer portal instead."
          : error;
        return;
      }

      if (!token) {
        this.errorMessage = "Google sign in did not return a token, please try again.";
        return;
      }

      this._partnerAuthService.completeExternalLogin(token, refreshToken);
      this._router.navigateByUrl(returnUrl || '/partner-dashboard');
    }
  }

  backToLogin() {
    this._router.navigateByUrl('/partner-auth/login');
  }

}
