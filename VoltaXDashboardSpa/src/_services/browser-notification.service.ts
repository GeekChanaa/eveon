import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { DashboardNotification } from 'src/_models/notification';
import { AccessService } from './access.service';

/** Actions also shown as a system (browser) notification: charge points waiting for their first configuration. */
export const BROWSER_NOTIFIED_ACTIONS = ['ChargePointAdded', 'ChargePointAwaitingProvisioning'];
const NAVIGABLE = /^\/(dashboard|partner-dashboard|my-dashboard)(\/|$)/;

/**
 * Mirrors selected dashboard notifications to the browser's Notification API, so operators see them
 * while the dashboard tab is in the background. The in-app toast covers the focused tab.
 */
@Injectable({
  providedIn: 'root'
})
export class BrowserNotificationService {
  private permissionAsked = false;

  constructor(private router: Router, private zone: NgZone, private access: AccessService) {}

  private get supported(): boolean { return typeof window !== 'undefined' && 'Notification' in window; }

  /**
   * Asks operators for the permission on their next click: browsers ignore or quietly block prompts
   * that are not triggered by a user gesture. Users who never receive these notifications are not asked.
   */
  requestPermissionOnNextClick(): void {
    if (!this.supported || this.permissionAsked || Notification.permission !== 'default') return;
    this.permissionAsked = true;
    const ask = () => {
      // Access is loaded after sign-in; until then, wait for a later click.
      if (!this.access.can('AccessDashboard') || !this.access.can('OperateChargePoints')) return;
      document.removeEventListener('click', ask, true);
      if (Notification.permission === 'default') Notification.requestPermission().catch(() => {});
    };
    // Outside Angular: the listener would otherwise run change detection on every click in the app.
    this.zone.runOutsideAngular(() => document.addEventListener('click', ask, true));
  }

  show(notification: DashboardNotification): void {
    if (!this.supported || Notification.permission !== 'granted') return;
    if (!BROWSER_NOTIFIED_ACTIONS.includes(notification.action || '')) return;
    if (document.visibilityState === 'visible' && document.hasFocus()) return;
    try {
      const system = new Notification('Charge point needs configuration', {
        body: notification.description || '',
        tag: 'notification-' + notification.id,
        requireInteraction: notification.urgent
      });
      system.onclick = () => {
        window.focus();
        system.close();
        if (NAVIGABLE.test(notification.url || '')) this.zone.run(() => this.router.navigateByUrl(notification.url));
      };
    } catch {
      // Some mobile browsers expose Notification but only allow it from a service worker.
    }
  }
}
