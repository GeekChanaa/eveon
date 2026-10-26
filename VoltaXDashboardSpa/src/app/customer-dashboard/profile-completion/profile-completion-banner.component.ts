import { Component, inject } from '@angular/core';
import { ProfileCompletionService } from 'src/_services/portal/profile-completion.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * What is missing on the customer's account (unverified email, missing or unverified phone), as a message on the
 * page. Always shown while something is missing, even after "Don't show again", which only stops the pop-up.
 */
@Component({
  selector: 'app-profile-completion-banner',
  standalone: true,
  imports: [PortalUiModule],
  template: `
<ng-container *ngIf="(completion.warnings$ | async) as warnings">
  <div class="vx-alert vx-alert--warning vx-completion-banner no-print" role="status" *ngIf="warnings.length">
    <vx-icon name="warning"></vx-icon>
    <div style="flex: 1">
      <strong>{{ 'portal.completion.bannerTitle' | translate }}</strong>
      <ul>
        <li *ngFor="let w of warnings">{{ ('portal.completion.warning.' + w) | translate }}</li>
      </ul>
    </div>
    <button type="button" class="vx-btn vx-btn--small vx-btn--primary" (click)="completion.open('contact')">
      {{ 'portal.completion.completeNow' | translate }}</button>
  </div>
</ng-container>`
})
export class ProfileCompletionBannerComponent {
  readonly completion = inject(ProfileCompletionService);
}
