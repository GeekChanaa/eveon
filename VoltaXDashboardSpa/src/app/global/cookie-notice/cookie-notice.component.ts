import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

const ACK_KEY = 'cookieNoticeAck';

// Informational only: the app sets strictly necessary cookies/storage, nothing that needs consent.
@Component({
  selector: 'app-cookie-notice',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="cookie-notice" *ngIf="visible" role="region" aria-label="Cookie notice">
      <p>
        We only use cookies and browser storage that are strictly necessary to keep you signed in and remember your
        settings. <a routerLink="/cookies">Learn more</a>
      </p>
      <button type="button" (click)="acknowledge()">OK</button>
    </div>
  `,
  styles: [`
    .cookie-notice { position: fixed; left: 16px; right: 16px; bottom: 16px; z-index: 10000; max-width: 720px; margin: 0 auto;
      display: flex; align-items: center; gap: 16px; padding: 14px 18px; border-radius: 12px;
      background: #171b24; color: #fff; box-shadow: 0 8px 24px rgba(0,0,0,.2); font-size: 13px; line-height: 1.5; }
    .cookie-notice p { margin: 0; flex: 1; }
    .cookie-notice a { color: #fff; text-decoration: underline; }
    .cookie-notice button { flex: none; padding: 8px 18px; border: none; border-radius: 8px; background: #fff; color: #171b24;
      font-weight: 600; cursor: pointer; }
  `]
})
export class CookieNoticeComponent {
  visible = !CookieNoticeComponent.acknowledged();

  acknowledge() {
    this.visible = false;
    try { localStorage.setItem(ACK_KEY, new Date().toISOString()); } catch { /* storage blocked: show again next visit */ }
  }

  private static acknowledged(): boolean {
    try { return !!localStorage.getItem(ACK_KEY); } catch { return false; }
  }
}
