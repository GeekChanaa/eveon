import { Component, DestroyRef, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from 'src/_services/auth.service';
import { CompletionStep, ProfileCompletionService, profileWarnings } from 'src/_services/portal/profile-completion.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { CustomerContactFormComponent } from './customer-contact-form.component';
import { CustomerDetailsFormComponent } from './customer-details-form.component';

/**
 * "Complete your account", in the customer dashboard shell. Pops up once per sign in while the email or the phone
 * is not verified, unless the customer chose "Don't show again"; opens on request (home page message,
 * notifications, profile) at any time. First the email and phone, then the optional details, which can be skipped.
 */
@Component({
  selector: 'app-profile-completion-dialog',
  standalone: true,
  imports: [PortalUiModule, CustomerContactFormComponent, CustomerDetailsFormComponent],
  template: `
<dialog #dialog class="vx-dialog vx-dialog--wide" aria-labelledby="completion-title" (cancel)="onEscape($event)">
  <ng-container *ngIf="completion.profile$ | async as profile">
    <div class="vx-dialog__body">
      <span class="vx-dialog__icon" aria-hidden="true"><svg><use [attr.href]="step === 'contact' ? '#icon-profile-circle' : '#icon-edit'"></use></svg></span>
      <div class="vx-dialog__content" style="flex: 1; min-width: 0">
        <span class="vx-eyebrow">{{ 'portal.completion.stepOf' | translate:{ step: step === 'contact' ? 1 : 2, total: 2 } }}</span>
        <h2 id="completion-title">{{ (step === 'contact' ? 'portal.completion.title' : 'portal.completion.detailsTitle') | translate }}</h2>
        <div class="vx-dialog__text">
          <p>{{ (step === 'contact' ? (warnings(profile).length ? 'portal.completion.intro' : 'portal.completion.contactDone') : 'portal.completion.detailsIntro') | translate }}</p>
        </div>
        <app-customer-contact-form *ngIf="step === 'contact'" [profile]="profile"></app-customer-contact-form>
        <app-customer-details-form *ngIf="step === 'details'" [profile]="profile" [skippable]="true"
          (done)="close()" (skipped)="close()"></app-customer-details-form>
      </div>
    </div>
    <div class="vx-dialog__actions" *ngIf="step === 'contact'">
      <button type="button" class="vx-btn vx-btn--link" style="margin-inline-end: auto" *ngIf="warnings(profile).length" (click)="dismissForever()">
        {{ 'portal.completion.dontShowAgain' | translate }}</button>
      <button type="button" class="vx-btn" (click)="close()">{{ 'portal.completion.later' | translate }}</button>
      <button type="button" class="vx-btn vx-btn--primary" (click)="step = 'details'">{{ 'portal.completion.continue' | translate }}</button>
    </div>
  </ng-container>
</dialog>`
})
export class ProfileCompletionDialogComponent implements OnInit {
  readonly completion = inject(ProfileCompletionService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild('dialog', { static: true }) private dialog!: ElementRef<HTMLDialogElement>;

  step: CompletionStep = 'contact';
  readonly warnings = profileWarnings;

  ngOnInit(): void {
    // Only the customer dashboard shows the warnings (notifications included): forget them when leaving it.
    this.destroyRef.onDestroy(() => this.completion.clear());
    this.completion.openRequests$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(step => this.open(step));
    this.completion.load().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (!this.completion.shouldPrompt(this.userId)) return;
        this.completion.markPrompted();
        this.open('contact');
      },
      error: () => { /* the profile page and home page show their own errors */ }
    });
  }

  open(step: CompletionStep): void {
    this.step = step;
    const dialog = this.dialog.nativeElement;
    if (dialog.open) return;
    if (typeof dialog.showModal === 'function') dialog.showModal();
    else dialog.setAttribute('open', '');
  }

  close(): void {
    const dialog = this.dialog.nativeElement;
    if (typeof dialog.close === 'function') dialog.close();
    else dialog.removeAttribute('open');
  }

  dismissForever(): void {
    this.completion.dismissForever(this.userId);
    this.close();
  }

  onEscape(event: Event): void {
    event.preventDefault();
    this.close();
  }

  private get userId(): number | null {
    const id = parseInt(this.auth.getAuthInformation()?.nameid, 10);
    return isNaN(id) ? null : id;
  }
}
