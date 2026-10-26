import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { switchMap } from 'rxjs';
import { MyProfile } from 'src/_models/portal';
import { AuthService } from 'src/_services/auth.service';
import { ProfileCompletionService } from 'src/_services/portal/profile-completion.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

/** A Moroccan mobile number (SMS codes only reach mobiles), in any of the shapes the API normalizes. */
const MOBILE = /^(?:\+212|00212|212|0)?[67]\d{8}$/;

/**
 * Email and phone of the signed-in customer, with what it takes to verify them: re-sending the email link, and
 * adding (or changing) the phone number then entering the code received by SMS. Emits changed once something was
 * verified; the shared profile is reloaded by then.
 */
@Component({
  selector: 'app-customer-contact-form',
  standalone: true,
  imports: [PortalUiModule],
  template: `
<div class="vx-contact" *ngIf="profile as p">
  <section class="vx-contact__row">
    <span class="vx-contact__icon"><vx-icon name="mail"></vx-icon></span>
    <div class="vx-contact__body">
      <div class="vx-contact__line">
        <strong>{{ 'customer.profile.email' | translate }}</strong>
        <span class="vx-badge" [ngClass]="p.isEmailVerified ? 'vx-badge--success' : 'vx-badge--warning'">
          {{ (p.isEmailVerified ? 'customer.profile.verified' : 'customer.profile.notVerified') | translate }}</span>
      </div>
      <div class="vx-contact__value">{{ p.email }}</div>
      <ng-container *ngIf="!p.isEmailVerified">
        <p class="vx-field__hint" *ngIf="!emailSent">{{ 'portal.completion.emailHint' | translate }}</p>
        <div class="vx-alert vx-alert--success" *ngIf="emailSent" role="status">{{ 'portal.completion.emailSent' | translate:{ email: p.email } }}</div>
        <div class="vx-alert vx-alert--error" *ngIf="emailError" role="alert">{{ emailError | translate }}</div>
        <div class="vx-actions">
          <button type="button" class="vx-btn vx-btn--small" [class.vx-btn--primary]="!emailSent" [disabled]="busy === 'email'" (click)="resendEmail()">
            {{ (emailSent ? 'portal.completion.resendEmail' : 'portal.completion.sendEmail') | translate }}</button>
          <button type="button" class="vx-btn vx-btn--small" *ngIf="emailSent" [disabled]="busy === 'refresh'" (click)="refresh()">
            {{ 'portal.completion.emailDone' | translate }}</button>
        </div>
      </ng-container>
    </div>
  </section>

  <section class="vx-contact__row">
    <span class="vx-contact__icon"><vx-icon name="phone"></vx-icon></span>
    <div class="vx-contact__body">
      <div class="vx-contact__line">
        <strong>{{ 'customer.profile.phone' | translate }}</strong>
        <span class="vx-badge" [ngClass]="!p.phone ? 'vx-badge--danger' : p.isPhoneNumberVerified ? 'vx-badge--success' : 'vx-badge--warning'">
          {{ (!p.phone ? 'portal.completion.missing' : p.isPhoneNumberVerified ? 'customer.profile.verified' : 'customer.profile.notVerified') | translate }}</span>
      </div>

      <ng-container *ngIf="p.isPhoneNumberVerified && !editingPhone; else phoneForm">
        <div class="vx-contact__value vx-num">{{ p.phone }}</div>
        <div class="vx-actions">
          <button type="button" class="vx-btn vx-btn--small" (click)="startPhoneChange()">{{ 'portal.completion.changePhone' | translate }}</button>
        </div>
      </ng-container>

      <ng-template #phoneForm>
        <p class="vx-field__hint">{{ (p.phone ? 'portal.completion.phoneVerifyHint' : 'portal.completion.phoneAddHint') | translate }}</p>
        <form class="vx-filters" (ngSubmit)="sendCode()" *ngIf="!codeSent">
          <div class="vx-field vx-field--grow">
            <label [for]="id + '-phone'">{{ 'portal.completion.phoneLabel' | translate }}</label>
            <input [id]="id + '-phone'" name="phone" type="tel" inputmode="tel" autocomplete="tel" dir="ltr"
              [(ngModel)]="phone" placeholder="06 12 34 56 78" required>
            <span class="vx-field__error" *ngIf="phoneTouched && !phoneValid">{{ 'portal.completion.phoneInvalid' | translate }}</span>
          </div>
          <button type="submit" class="vx-btn vx-btn--primary" [disabled]="busy === 'sms'">{{ 'portal.completion.sendCode' | translate }}</button>
          <button type="button" class="vx-btn" *ngIf="editingPhone" (click)="cancelPhoneChange()">{{ 'common.actions.cancel' | translate }}</button>
        </form>
        <form class="vx-filters" (ngSubmit)="verifyCode()" *ngIf="codeSent">
          <div class="vx-field vx-field--grow">
            <label [for]="id + '-code'">{{ 'portal.completion.codeLabel' | translate:{ phone: sentTo } }}</label>
            <input [id]="id + '-code'" name="code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" dir="ltr"
              [(ngModel)]="code" required>
          </div>
          <button type="submit" class="vx-btn vx-btn--primary" [disabled]="busy === 'verify' || !code.trim()">{{ 'portal.completion.verify' | translate }}</button>
          <button type="button" class="vx-btn" (click)="codeSent = false; code = ''">{{ 'portal.completion.otherNumber' | translate }}</button>
        </form>
        <div class="vx-alert vx-alert--error" *ngIf="phoneError" role="alert">{{ phoneError | translate }}</div>
      </ng-template>
      <div class="vx-alert vx-alert--success" *ngIf="phoneVerified" role="status">{{ 'portal.completion.phoneVerified' | translate }}</div>
    </div>
  </section>
</div>`
})
export class CustomerContactFormComponent implements OnChanges {
  private static nextId = 0;
  @Input() profile: MyProfile | null = null;
  @Output() changed = new EventEmitter<void>();

  readonly id = `vx-contact-${++CustomerContactFormComponent.nextId}`;
  busy: 'email' | 'refresh' | 'sms' | 'verify' | null = null;
  emailSent = false;
  emailError: string | null = null;
  editingPhone = false;
  phone = '';
  phoneTouched = false;
  codeSent = false;
  sentTo = '';
  code = '';
  phoneError: string | null = null;
  phoneVerified = false;

  constructor(private auth: AuthService, private completion: ProfileCompletionService) {}

  ngOnChanges(): void {
    if (!this.codeSent && !this.editingPhone) this.phone = this.profile?.phone ?? '';
  }

  get phoneValid(): boolean { return MOBILE.test(this.phone.replace(/[\s.\-()]/g, '')); }

  resendEmail(): void {
    this.busy = 'email';
    this.emailError = null;
    this.auth.resendVerificationEmail().subscribe({
      next: () => { this.busy = null; this.emailSent = true; },
      error: (e: HttpErrorResponse) => { this.busy = null; this.emailError = e.status === 429 ? 'portal.completion.tooMany' : 'common.state.error'; }
    });
  }

  /** The customer opened the link in their mailbox: read the profile again to pick the verification up. */
  refresh(): void {
    this.busy = 'refresh';
    this.completion.load().subscribe({
      next: profile => {
        this.busy = null;
        if (profile.isEmailVerified) this.changed.emit();
        else this.emailError = 'portal.completion.emailStillPending';
      },
      error: () => { this.busy = null; this.emailError = 'common.state.error'; }
    });
  }

  startPhoneChange(): void {
    this.editingPhone = true;
    this.phoneVerified = false;
    this.phone = '';
  }

  cancelPhoneChange(): void {
    this.editingPhone = false;
    this.codeSent = false;
    this.phoneError = null;
    this.phone = this.profile?.phone ?? '';
  }

  sendCode(): void {
    this.phoneTouched = true;
    if (!this.phoneValid || !this.profile) return;
    this.busy = 'sms';
    this.phoneError = null;
    this.auth.sendPhoneVerificationSms({ phone: this.phone.trim(), email: this.profile.email }).subscribe({
      next: () => { this.busy = null; this.codeSent = true; this.sentTo = this.phone.trim(); this.code = ''; },
      error: (e: HttpErrorResponse) => {
        this.busy = null;
        this.phoneError = e.status === 400 ? 'portal.completion.phoneTaken' : e.status === 429 ? 'portal.completion.tooMany' : 'portal.completion.smsFailed';
      }
    });
  }

  verifyCode(): void {
    if (!this.profile) return;
    this.busy = 'verify';
    this.phoneError = null;
    this.auth.verifyPhone({ email: this.profile.email, token: this.code.trim() })
      .pipe(switchMap(() => this.completion.load()))
      .subscribe({
        next: () => {
          this.busy = null;
          this.codeSent = false;
          this.editingPhone = false;
          this.code = '';
          this.phoneVerified = true;
          this.changed.emit();
        },
        error: (e: HttpErrorResponse) => {
          this.busy = null;
          this.phoneError = e.status === 400 || e.status === 500 ? 'portal.completion.wrongCode' : e.status === 429 ? 'portal.completion.tooMany' : 'common.state.error';
        }
      });
  }
}
