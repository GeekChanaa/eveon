import { Component, OnInit } from '@angular/core';
import { AuthService, TwoFactorEnrollment, TwoFactorStatus } from 'src/_services/auth.service';

/**
 * Enroll / disable TOTP two factor authentication. Used in the profile Security tab and
 * on the partner portal security page.
 */
@Component({
  selector: 'app-two-factor-settings',
  templateUrl: './two-factor-settings.component.html',
  styleUrls: ['./two-factor-settings.component.sass']
})
export class TwoFactorSettingsComponent implements OnInit {

  status: TwoFactorStatus | null = null;
  enrollment: TwoFactorEnrollment | null = null;
  recoveryCodes: string[] = [];
  code = '';
  password = '';
  disabling = false;
  isLoading = false;
  error = '';

  constructor(private _authService: AuthService) { }

  ngOnInit() {
    this.loadStatus();
  }

  loadStatus() {
    this._authService.getTwoFactorStatus().subscribe({
      next: status => this.status = status,
      error: () => this.error = 'Could not load your two factor authentication settings.'
    });
  }

  startEnrollment() {
    this.run(this._authService.enrollTwoFactor(), enrollment => {
      this.enrollment = enrollment;
      this.code = '';
    });
  }

  confirmEnrollment() {
    this.run(this._authService.confirmTwoFactor(this.code.trim()), result => {
      this.recoveryCodes = result.recoveryCodes;
      this.enrollment = null;
      this.code = '';
      this.loadStatus();
    });
  }

  disable() {
    this.run(this._authService.disableTwoFactor(this.password, this.code.trim()), () => {
      this.disabling = false;
      this.password = '';
      this.code = '';
      this.recoveryCodes = [];
      this.loadStatus();
    });
  }

  cancel() {
    this.enrollment = null;
    this.disabling = false;
    this.code = '';
    this.password = '';
    this.error = '';
  }

  copyRecoveryCodes() {
    navigator.clipboard?.writeText(this.recoveryCodes.join('\n'));
  }

  private run<T>(request: import('rxjs').Observable<T>, done: (result: T) => void) {
    this.isLoading = true;
    this.error = '';
    request.subscribe({
      next: result => { this.isLoading = false; done(result); },
      error: err => {
        this.isLoading = false;
        this.error = err.status == 429 ? 'Too many attempts. Please wait a minute.' : err.error?.error ?? 'Something went wrong, please try again.';
      }
    });
  }
}
