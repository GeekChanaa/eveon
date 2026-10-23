import { Component } from '@angular/core';

/** Security of the partner account: two-factor authentication, in the portal's page layout. */
@Component({
  selector: 'app-partner-security',
  template: `
    <div class="page__inner"><main class="page__container">
      <header class="vx-heading">
        <div>
          <span class="vx-eyebrow">{{ 'partner.eyebrow' | translate }}</span>
          <h1>{{ 'nav.security' | translate }}</h1>
          <p>{{ 'portal.partner.securitySubtitle' | translate }}</p>
        </div>
      </header>
      <section class="vx-panel">
        <app-two-factor-settings></app-two-factor-settings>
      </section>
      <p class="vx-footnote">{{ 'portal.partner.securityNote' | translate }}</p>
    </main></div>`
})
export class PartnerSecurityComponent {}
