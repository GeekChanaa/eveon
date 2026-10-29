import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { LinkedAccountsDto } from 'src/_models/_dtos/linked-accounts-dto';
import { AuthService } from 'src/_services/auth.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

/** Where the Google link flow comes back to (AuthController.GoogleLink, then auth/google-callback). */
export const SECURITY_RETURN_URL = '/my-dashboard/profile?google=linked';

/** Same rule as the API's [StrongPassword]: 8+ characters with upper and lower case, a digit and a special character. */
export function isStrongPassword(value: string): boolean {
  return value.length >= 8 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /[0-9]/.test(value)
    && /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value);
}

/**
 * How the signed-in customer signs in: changing the password (the current one is required; every session ends
 * afterwards, this one included), or setting one through the reset email when the account was created with Google;
 * and linking or unlinking a Google account. Unlinking needs a password, otherwise the customer would be locked out.
 */
@Component({
  selector: 'app-customer-security-form',
  standalone: true,
  imports: [PortalUiModule],
  template: `
<div class="vx-contact" *ngIf="accounts as a; else loadingTpl">
  <section class="vx-contact__row">
    <span class="vx-contact__icon"><vx-icon name="lock"></vx-icon></span>
    <div class="vx-contact__body">
      <div class="vx-contact__line">
        <strong>{{ 'portal.security.password' | translate }}</strong>
        <span class="vx-badge" [ngClass]="a.hasPassword ? 'vx-badge--success' : 'vx-badge--warning'">
          {{ (a.hasPassword ? 'portal.security.passwordSet' : 'portal.security.passwordNotSet') | translate }}</span>
      </div>

      <ng-container *ngIf="a.hasPassword; else setPassword">
        <ng-container *ngIf="!editingPassword">
          <p class="vx-field__hint">{{ 'portal.security.changeHint' | translate }}</p>
          <div class="vx-actions">
            <button type="button" class="vx-btn vx-btn--small" (click)="startPasswordChange()">{{ 'portal.security.change' | translate }}</button>
          </div>
        </ng-container>

        <form *ngIf="editingPassword" (ngSubmit)="changePassword()" autocomplete="on">
          <!-- Lets password managers file the new password under the right account. -->
          <input type="email" name="username" autocomplete="username" [value]="email" hidden readonly>
          <div class="vx-field">
            <label [for]="id + '-current'">{{ 'portal.security.current' | translate }}</label>
            <input [id]="id + '-current'" name="current" type="password" autocomplete="current-password" dir="ltr" maxlength="128"
              [(ngModel)]="current" required>
          </div>
          <div class="vx-field">
            <label [for]="id + '-new'">{{ 'portal.security.new' | translate }}</label>
            <input [id]="id + '-new'" name="new" type="password" autocomplete="new-password" dir="ltr" maxlength="128"
              [(ngModel)]="next" required>
            <span class="vx-field__hint" *ngIf="!submitted || strong">{{ 'portal.security.rules' | translate }}</span>
            <span class="vx-field__error" *ngIf="submitted && !strong">{{ 'portal.security.weak' | translate }}</span>
            <span class="vx-field__error" *ngIf="submitted && strong && sameAsCurrent">{{ 'portal.security.sameAsCurrent' | translate }}</span>
          </div>
          <div class="vx-field">
            <label [for]="id + '-confirm'">{{ 'portal.security.confirm' | translate }}</label>
            <input [id]="id + '-confirm'" name="confirm" type="password" autocomplete="new-password" dir="ltr" maxlength="128"
              [(ngModel)]="confirm" required>
            <span class="vx-field__error" *ngIf="submitted && !matches">{{ 'portal.security.mismatch' | translate }}</span>
          </div>
          <p class="vx-field__hint">{{ 'portal.security.signOutNotice' | translate }}</p>
          <div class="vx-alert vx-alert--error" *ngIf="passwordError" role="alert">{{ passwordError | translate }}</div>
          <div class="vx-actions">
            <button type="submit" class="vx-btn vx-btn--primary vx-btn--small" [disabled]="busy === 'password'">{{ 'portal.security.save' | translate }}</button>
            <button type="button" class="vx-btn vx-btn--small" [disabled]="busy === 'password'" (click)="cancelPasswordChange()">{{ 'common.actions.cancel' | translate }}</button>
          </div>
        </form>
      </ng-container>

      <ng-template #setPassword>
        <p class="vx-field__hint" *ngIf="!resetSent">{{ 'portal.security.setHint' | translate }}</p>
        <div class="vx-alert vx-alert--success" *ngIf="resetSent" role="status">{{ 'portal.security.setSent' | translate:{ email: email } }}</div>
        <div class="vx-alert vx-alert--error" *ngIf="passwordError" role="alert">{{ passwordError | translate }}</div>
        <div class="vx-actions">
          <button type="button" class="vx-btn vx-btn--small" [class.vx-btn--primary]="!resetSent" [disabled]="busy === 'reset' || !email" (click)="sendSetPasswordEmail()">
            {{ (resetSent ? 'portal.security.setResend' : 'portal.security.set') | translate }}</button>
        </div>
      </ng-template>
    </div>
  </section>

  <section class="vx-contact__row">
    <span class="vx-contact__icon"><vx-icon name="link"></vx-icon></span>
    <div class="vx-contact__body">
      <div class="vx-contact__line">
        <strong>{{ 'portal.security.google' | translate }}</strong>
        <span class="vx-badge" [ngClass]="a.googleLinked ? 'vx-badge--success' : 'vx-badge--info'">
          {{ (a.googleLinked ? 'portal.security.linked' : 'portal.security.notLinked') | translate }}</span>
      </div>
      <div class="vx-contact__value" *ngIf="a.googleLinked && a.googleEmail">{{ a.googleEmail }}</div>
      <div class="vx-alert vx-alert--success" *ngIf="justLinked && a.googleLinked" role="status">{{ 'portal.security.linkedDone' | translate }}</div>
      <div class="vx-alert vx-alert--error" *ngIf="googleError" role="alert">{{ googleError | translate }}</div>

      <ng-container *ngIf="!a.googleLinked">
        <p class="vx-field__hint">{{ 'portal.security.linkHint' | translate }}</p>
        <div class="vx-actions">
          <button type="button" class="vx-btn vx-btn--primary vx-btn--small" [disabled]="busy === 'link'" (click)="linkGoogle()">{{ 'portal.security.link' | translate }}</button>
        </div>
      </ng-container>

      <ng-container *ngIf="a.googleLinked">
        <p class="vx-field__hint">{{ (a.hasPassword ? 'portal.security.unlinkHint' : 'portal.security.unlinkNeedsPassword') | translate }}</p>
        <div class="vx-actions" *ngIf="!confirmingUnlink">
          <button type="button" class="vx-btn vx-btn--small" [disabled]="!a.hasPassword" (click)="confirmingUnlink = true">{{ 'portal.security.unlink' | translate }}</button>
        </div>
        <div class="vx-actions" *ngIf="confirmingUnlink">
          <span class="vx-field__hint">{{ 'portal.security.unlinkConfirm' | translate }}</span>
          <button type="button" class="vx-btn vx-btn--danger vx-btn--small" [disabled]="busy === 'unlink'" (click)="unlinkGoogle()">{{ 'portal.security.unlinkYes' | translate }}</button>
          <button type="button" class="vx-btn vx-btn--small" [disabled]="busy === 'unlink'" (click)="confirmingUnlink = false">{{ 'common.actions.cancel' | translate }}</button>
        </div>
      </ng-container>
    </div>
  </section>
</div>
<ng-template #loadingTpl>
  <p class="vx-empty" *ngIf="!loadError">{{ 'common.state.loading' | translate }}</p>
  <div class="vx-alert vx-alert--error" *ngIf="loadError" role="alert">{{ 'common.state.error' | translate }}</div>
</ng-template>`
})
export class CustomerSecurityFormComponent implements OnInit {
  private static nextId = 0;
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  /** The customer's email: sign in name for password managers, and where the set password link goes. */
  @Input() email = '';

  readonly id = `vx-security-${++CustomerSecurityFormComponent.nextId}`;
  accounts: LinkedAccountsDto | null = null;
  loadError = false;
  busy: 'password' | 'reset' | 'link' | 'unlink' | null = null;

  editingPassword = false;
  current = '';
  next = '';
  confirm = '';
  submitted = false;
  passwordError: string | null = null;
  resetSent = false;

  justLinked = false;
  confirmingUnlink = false;
  googleError: string | null = null;

  ngOnInit(): void {
    this.justLinked = this.route.snapshot.queryParamMap.get('google') === 'linked';
    this.auth.getLinkedAccounts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: accounts => this.accounts = accounts,
      error: () => this.loadError = true
    });
  }

  get strong(): boolean { return isStrongPassword(this.next); }
  get matches(): boolean { return this.next === this.confirm; }
  get sameAsCurrent(): boolean { return this.next !== '' && this.next === this.current; }

  startPasswordChange(): void {
    this.editingPassword = true;
    this.passwordError = null;
  }

  cancelPasswordChange(): void {
    this.editingPassword = false;
    this.submitted = false;
    this.passwordError = null;
    this.current = this.next = this.confirm = '';
  }

  /** On success the API ends every session, so AuthService.changePassword signs this one out as well. */
  changePassword(): void {
    this.submitted = true;
    this.passwordError = null;
    if (!this.current || !this.strong || !this.matches || this.sameAsCurrent) return;
    const userID = parseInt(this.auth.getAuthInformation()?.nameid, 10);
    if (!userID) { this.passwordError = 'common.state.error'; return; }
    this.busy = 'password';
    this.auth.changePassword({ id: userID, currentPassword: this.current, newPassword: this.next, newPasswordCheck: this.confirm }).subscribe({
      next: () => { this.busy = null; this.current = this.next = this.confirm = ''; },
      error: (e: HttpErrorResponse) => {
        this.busy = null;
        this.current = '';
        this.passwordError = e.status === 403 ? 'portal.security.wrongCurrent'
          : e.status === 429 ? 'portal.completion.tooMany'
          : e.status === 400 && String(e.error ?? '').includes('different') ? 'portal.security.sameAsCurrent'
          : e.status === 400 ? 'portal.security.invalid'
          : 'common.state.error';
      }
    });
  }

  /** Accounts created with Google have no password: the reset email lets the customer choose one. */
  sendSetPasswordEmail(): void {
    this.busy = 'reset';
    this.passwordError = null;
    this.auth.resetPasswordRequest(this.email).subscribe({
      next: () => { this.busy = null; this.resetSent = true; },
      error: (e: HttpErrorResponse) => { this.busy = null; this.passwordError = e.status === 429 ? 'portal.completion.tooMany' : 'common.state.error'; }
    });
  }

  /** Leaves the page for Google; the callback brings the customer back to SECURITY_RETURN_URL. */
  linkGoogle(): void {
    this.busy = 'link';
    this.googleError = null;
    this.auth.startGoogleLink(SECURITY_RETURN_URL);
  }

  unlinkGoogle(): void {
    this.busy = 'unlink';
    this.googleError = null;
    this.auth.unlinkGoogle().subscribe({
      next: accounts => { this.busy = null; this.confirmingUnlink = false; this.justLinked = false; this.accounts = accounts; },
      error: (e: HttpErrorResponse) => {
        this.busy = null;
        this.confirmingUnlink = false;
        this.googleError = e.status === 400 ? 'portal.security.unlinkNeedsPassword' : 'common.state.error';
      }
    });
  }
}
