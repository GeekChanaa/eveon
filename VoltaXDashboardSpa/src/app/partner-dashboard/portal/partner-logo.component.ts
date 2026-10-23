import { Component, Input } from '@angular/core';
import { PartnerProfile } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';

/** The partner's logo, or its initials when it has not uploaded one. */
@Component({
  selector: 'app-partner-logo',
  template: `
    <span class="vx-partner-logo" [class.vx-partner-logo--large]="size === 'large'">
      <img *ngIf="url; else initialsTpl" [src]="url" [alt]="profile?.name ?? ''">
      <ng-template #initialsTpl><span aria-hidden="true">{{ initials }}</span></ng-template>
    </span>`
})
export class PartnerLogoComponent {
  @Input() profile: PartnerProfile | null = null;
  @Input() size: 'normal' | 'large' = 'normal';
  /** Overrides the stored logo (local preview before upload). */
  @Input() preview: string | null = null;

  get url(): string | null { return this.preview ?? PartnerPortalService.fileUrl(this.profile?.logoUrl); }

  get initials(): string {
    const words = (this.profile?.name ?? '').trim().split(/\s+/).filter(Boolean);
    return words.slice(0, 2).map(w => w[0].toUpperCase()).join('') || '·';
  }
}
