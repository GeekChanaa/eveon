import { Component, HostListener, Input } from '@angular/core';

/**
 * Brand header of portal pages: eveon's logo, the partner's logo on partner pages, the document title and who it
 * is for. On screen it follows the theme (white logo, light text in dark mode); the print date shows on paper only.
 */
@Component({
  selector: 'vx-print-header',
  template: `
    <header class="vx-print-header print-only">
      <div class="vx-print-header__brands">
        <img class="vx-print-header__eveon" src="/assets/images/logos/logo.svg" alt="eveon">
        <img class="vx-print-header__eveon vx-print-header__eveon--on-dark" src="/assets/images/logos/logo-white.svg" alt="">
        <img *ngIf="partnerLogo" class="vx-print-header__partner" [src]="partnerLogo" [alt]="partnerName ?? ''">
        <span *ngIf="!partnerLogo && partnerName" class="vx-print-header__partner-name">{{ partnerName }}</span>
      </div>
      <div class="vx-print-header__meta">
        <strong>{{ title }}</strong>
        <span *ngIf="holder">{{ holder }}</span>
        <span *ngIf="subtitle">{{ subtitle }}</span>
        <span class="print-only">{{ 'portal.print.printedAt' | translate:{ date: (printedAt | vxDate:'medium') } }}</span>
      </div>
    </header>`
})
export class VxPrintHeaderComponent {
  @Input() title = '';
  @Input() subtitle: string | null = null;
  /** A partner's logo, printed next to eveon's on partner portal pages. */
  @Input() partnerLogo: string | null = null;
  /** The partner's name, printed when it has no logo. */
  @Input() partnerName: string | null = null;
  /** Whose document it is (customer name, partner name). */
  @Input() holder: string | null = null;
  printedAt = new Date();

  /** The page may stay open for hours: the date printed is the one of the print. */
  @HostListener('window:beforeprint')
  refresh(): void { this.printedAt = new Date(); }
}

/** Last line of a printed page: where it comes from and what the amounts mean. */
@Component({
  selector: 'vx-print-footer',
  template: `
    <footer class="vx-print-footer print-only">
      <span>{{ note || ('portal.print.footer' | translate) }}</span>
      <span>eveon.ma</span>
    </footer>`
})
export class VxPrintFooterComponent {
  @Input() note: string | null = null;
}
