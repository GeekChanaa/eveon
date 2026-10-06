import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { PartnerAuthService } from 'src/_services/partner-auth.service';
import { passwordMatchValidator } from 'src/app/validators/password-match-validator';
import { strongPasswordValidator } from 'src/app/validators/strong-password-validator';

/** Landing page of the partner reset link: email + single use token come from the query string. */
@Component({
  selector: 'app-partner-reset-password',
  templateUrl: './partner-reset-password.component.html',
  styleUrls: ['../../auth/reset-password/reset-password.component.sass']
})
export class PartnerResetPasswordComponent implements OnInit {

  form: FormGroup;
  isLoading = false;
  errorMessage = '';
  private email = '';
  private token = '';

  constructor(
    private _partnerAuthService: PartnerAuthService,
    private _route: ActivatedRoute,
    private _modalService: ActionModalService,
    private _router: Router
  ) {
    this.form = new FormGroup({
      password: new FormControl('', [Validators.required, Validators.minLength(8), strongPasswordValidator()]),
      confirmPassword: new FormControl('', [Validators.required])
    }, {
      validators: passwordMatchValidator('password', 'confirmPassword')
    });
  }

  ngOnInit() {
    const params = this._route.snapshot.queryParamMap;
    this.email = params.get('email') ?? '';
    this.token = params.get('token') ?? '';
    // Keep the token out of the history and of any later Referer header.
    window.history.replaceState(window.history.state, '', window.location.pathname);
    if (!this.email || !this.token) {
      this.errorMessage = 'This reset link is incomplete. Please request a new one.';
    }
  }

  resetPassword() {
    if (this.form.invalid || !this.token) return;
    this.isLoading = true;
    this.errorMessage = '';
    this._partnerAuthService.resetPassword({ email: this.email, token: this.token, password: this.form.value.password }).subscribe({
      next: () => {
        this.isLoading = false;
        this._modalService.popup(ActionModalStatusEnum.Success, "Password updated", "You can now sign in with your new password", 4000);
        this._router.navigateByUrl('/partner-auth/login');
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.status == 429 ? 'Too many attempts. Please wait a minute.'
          : error.error?.error ?? 'Something went wrong, please try again later';
      }
    });
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }
}
