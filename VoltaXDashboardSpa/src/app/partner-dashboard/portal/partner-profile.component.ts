import { Component, DestroyRef, OnDestroy, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PartnerProfile } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';

const LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp'];
const LOGO_MAX_BYTES = 5 * 1024 * 1024;

/** The partner's details (read only) and its logo, which the partner can replace. */
@Component({
  selector: 'app-partner-profile',
  template: `
    <div class="page__inner"><main class="page__container">
      <header class="vx-heading">
        <div>
          <span class="vx-eyebrow">{{ 'partner.eyebrow' | translate }}</span>
          <h1>{{ 'partner.profile.title' | translate }}</h1>
          <p>{{ 'partner.profile.subtitle' | translate }}</p>
        </div>
      </header>

      <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ error | translate }}</div>
      <div class="vx-alert vx-alert--success" *ngIf="saved" role="status">{{ 'partner.profile.logoSaved' | translate }}</div>

      <div class="vx-split" *ngIf="profile">
        <section class="vx-panel">
          <div class="vx-panel__head"><div><h2>{{ profile.name }}</h2><p>{{ 'partner.profile.readOnly' | translate }}</p></div></div>
          <dl class="vx-kv">
            <div><dt>{{ 'partner.profile.email' | translate }}</dt><dd>{{ profile.email || '—' }}</dd></div>
            <div><dt>{{ 'partner.profile.phone' | translate }}</dt><dd>{{ profile.phone || '—' }}</dd></div>
            <div><dt>{{ 'partner.profile.city' | translate }}</dt><dd>{{ profile.city || '—' }}</dd></div>
            <div><dt>{{ 'partner.fields.address' | translate }}</dt><dd>{{ profile.address || '—' }}</dd></div>
            <div><dt>{{ 'partner.stations.title' | translate }}</dt><dd class="vx-num">{{ profile.stationCount }}</dd></div>
            <div><dt>{{ 'partner.stations.chargePoints' | translate }}</dt><dd class="vx-num">{{ profile.chargePointCount }}</dd></div>
          </dl>
          <p class="vx-footnote"><a routerLink="/partner-dashboard/security">{{ 'partner.profile.security' | translate }}</a></p>
        </section>

        <section class="vx-panel">
          <div class="vx-panel__head"><div><h2>{{ 'partner.profile.logo' | translate }}</h2><p>{{ 'partner.profile.logoHint' | translate }}</p></div></div>
          <div class="vx-logo-upload">
            <app-partner-logo [profile]="profile" [preview]="preview" size="large"></app-partner-logo>
            <input #file type="file" id="logo" class="vx-sr-only" accept="image/png,image/jpeg,image/webp" (change)="pick($any($event.target).files)">
            <div class="vx-actions">
              <label for="logo" class="vx-btn">{{ 'partner.profile.choose' | translate }}</label>
              <button type="button" class="vx-btn vx-btn--primary" *ngIf="selected" [disabled]="uploading" (click)="upload(); file.value = ''">
                {{ (uploading ? 'partner.profile.uploading' : 'partner.profile.upload') | translate }}
              </button>
              <button type="button" class="vx-btn" *ngIf="selected && !uploading" (click)="clear(); file.value = ''">{{ 'common.actions.cancel' | translate }}</button>
            </div>
          </div>
        </section>
      </div>
    </main></div>`
})
export class PartnerProfileComponent implements OnInit, OnDestroy {
  private readonly portal = inject(PartnerPortalService);
  private readonly destroyRef = inject(DestroyRef);

  profile: PartnerProfile | null = null;
  selected: File | null = null;
  preview: string | null = null;
  uploading = false;
  saved = false;
  error: string | null = null;

  ngOnInit(): void {
    this.portal.profile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: profile => this.profile = profile,
      error: () => this.error = 'common.state.error'
    });
  }

  pick(files: FileList | null): void {
    this.saved = false;
    this.error = null;
    const file = files?.[0];
    if (!file) return;
    if (!LOGO_TYPES.includes(file.type)) { this.error = 'partner.profile.badType'; return; }
    if (file.size > LOGO_MAX_BYTES) { this.error = 'partner.profile.tooBig'; return; }
    this.clear();
    this.selected = file;
    this.preview = URL.createObjectURL(file);
  }

  upload(): void {
    if (!this.selected) return;
    this.uploading = true;
    this.portal.uploadLogo(this.selected).subscribe({
      next: profile => { this.profile = profile; this.uploading = false; this.saved = true; this.clear(); },
      error: () => { this.uploading = false; this.error = 'partner.profile.uploadFailed'; }
    });
  }

  clear(): void {
    if (this.preview) URL.revokeObjectURL(this.preview);
    this.preview = null;
    this.selected = null;
  }

  ngOnDestroy(): void { this.clear(); }
}
