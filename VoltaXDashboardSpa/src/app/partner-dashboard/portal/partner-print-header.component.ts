import { Component, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PartnerProfile } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';

/** The printed header of partner portal pages: eveon's logo next to the partner's (or its name), and the title. */
@Component({
  selector: 'app-partner-print-header',
  template: `<vx-print-header [title]="title" [subtitle]="subtitle" [partnerLogo]="logo" [partnerName]="profile?.name ?? null"
    [holder]="profile?.name ?? null"></vx-print-header>`
})
export class PartnerPrintHeaderComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);

  @Input() title = '';
  @Input() subtitle: string | null = null;
  profile: PartnerProfile | null = null;

  constructor() {
    this.portal.profile$.pipe(takeUntilDestroyed()).subscribe(profile => this.profile = profile);
  }

  ngOnInit(): void {
    // The sidebar loads the profile; a page opened on its own still gets the logo.
    if (!this.profile) this.portal.profile().subscribe({ error: () => undefined });
  }

  get logo(): string | null { return PartnerPortalService.fileUrl(this.profile?.logoUrl); }
}
