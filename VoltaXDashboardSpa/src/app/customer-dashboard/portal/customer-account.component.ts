import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { avatarUrl } from 'src/_helpers/avatar-url';
import { MyCard, MyProfile } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { ProfileCompletionService } from 'src/_services/portal/profile-completion.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** Charging cards (RFID) of the customer, read only. */
@Component({
  selector: 'app-customer-cards',
  template: `
<div class="page__inner"><main class="page__container">
  <vx-print-header [title]="'nav.myCards' | translate"></vx-print-header>
  <header class="vx-heading">
    <div>
      <span class="vx-eyebrow">{{ 'customer.eyebrow' | translate }}</span>
      <h1>{{ 'nav.myCards' | translate }}</h1>
      <p>{{ 'customer.cards.subtitle' | translate }}</p>
    </div>
  </header>
  <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ 'common.state.error' | translate }}</div>
  <section class="vx-panel">
    <div class="vx-table-wrap" *ngIf="cards.length; else none">
      <table class="vx-table">
        <thead><tr>
          <th>{{ 'customer.cards.number' | translate }}</th>
          <th class="num">{{ 'customer.overview.balance' | translate }}</th>
          <th>{{ 'customer.cards.expires' | translate }}</th>
          <th>{{ 'common.fields.status' | translate }}</th>
        </tr></thead>
        <tbody>
          <tr *ngFor="let c of cards">
            <td class="vx-num">{{ c.maskedNumber }}</td>
            <td class="num">{{ c.balance | vxMoney }}</td>
            <td>{{ c.expirationDate | vxDate:'mediumDate' }}</td>
            <td><span class="vx-badge" [ngClass]="tone(state(c))">{{ ('customer.cards.state.' + state(c)) | translate }}</span></td>
          </tr>
        </tbody>
      </table>
    </div>
    <ng-template #none><p class="vx-empty">{{ (loading ? 'common.state.loading' : 'customer.cards.empty') | translate }}</p></ng-template>
    <p class="muted" style="margin-block-start: 14px; font-size: 12px">{{ 'customer.cards.lostCard' | translate }}</p>
  </section>
</main></div>`
})
export class CustomerCardsComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly destroyRef = inject(DestroyRef);
  cards: MyCard[] = [];
  loading = true;
  error = false;
  readonly tone = statusTone;

  ngOnInit(): void {
    this.portal.cards().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: cards => { this.cards = cards; this.loading = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  state(card: MyCard): 'Active' | 'Blocked' | 'Expired' {
    return card.blocked ? 'Blocked' : card.expired ? 'Expired' : 'Active';
  }
}

/**
 * The customer's profile: name and photo (changed through support), email and phone with their verification, the
 * optional details the customer fills in, and how they sign in (password, Google).
 */
@Component({
  selector: 'app-customer-profile',
  template: `
<div class="page__inner"><main class="page__container">
  <header class="vx-heading">
    <div>
      <span class="vx-eyebrow">{{ 'customer.eyebrow' | translate }}</span>
      <h1>{{ 'header.myProfile' | translate }}</h1>
      <p>{{ 'customer.profile.subtitle' | translate }}</p>
    </div>
  </header>
  <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ 'common.state.error' | translate }}</div>
  <app-profile-completion-banner></app-profile-completion-banner>
  <ng-container *ngIf="profile$ | async as p">
    <section class="vx-panel">
      <div class="vx-panel__head" style="justify-content: flex-start; gap: 16px">
        <span class="account-avatar" style="width: 56px; height: 56px; display: grid; place-items: center; border-radius: 50%; overflow: hidden; background: var(--vx-brand-soft); color: var(--vx-brand-ink); font-weight: 700">
          <img *ngIf="photo(p) as src; else initials" [src]="src" alt="" style="width: 100%; height: 100%; object-fit: cover">
          <ng-template #initials>{{ (p.firstName.charAt(0) || '') + (p.lastName.charAt(0) || '') }}</ng-template>
        </span>
        <div><h2>{{ p.firstName }} {{ p.lastName }}</h2><p>{{ 'customer.profile.memberSince' | translate:{ date: (p.memberSince | vxDate:'mediumDate') } }}</p></div>
      </div>
      <p class="muted" style="margin: 0; font-size: 12px">{{ 'portal.completion.nameReadOnly' | translate }}</p>
    </section>

    <section class="vx-panel">
      <div class="vx-panel__head"><div><h2>{{ 'portal.completion.contactTitle' | translate }}</h2><p>{{ 'portal.completion.contactHint' | translate }}</p></div></div>
      <app-customer-contact-form [profile]="p"></app-customer-contact-form>
    </section>

    <section class="vx-panel">
      <div class="vx-panel__head"><div><h2>{{ 'portal.completion.detailsTitle' | translate }}</h2><p>{{ 'portal.completion.detailsIntro' | translate }}</p></div></div>
      <app-customer-details-form [profile]="p"></app-customer-details-form>
    </section>

    <section class="vx-panel">
      <div class="vx-panel__head"><div><h2>{{ 'portal.security.title' | translate }}</h2><p>{{ 'portal.security.intro' | translate }}</p></div></div>
      <app-customer-security-form [email]="p.email"></app-customer-security-form>
    </section>
  </ng-container>
</main></div>`
})
export class CustomerProfileComponent implements OnInit {
  private readonly completion = inject(ProfileCompletionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly profile$ = this.completion.profile$;
  error = false;

  ngOnInit(): void {
    this.completion.load().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => this.error = true });
  }

  photo(profile: MyProfile): string | null { return avatarUrl(profile.imageUrl); }
}
