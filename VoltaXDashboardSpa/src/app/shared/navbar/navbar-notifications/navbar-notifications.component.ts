import { Component, ElementRef, NgZone, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { DashboardNotification } from 'src/_models/notification';
import { ConfirmService } from 'src/_services/confirm.service';
import { NotificationService, NotificationState } from 'src/_services/notification.service';

type Tone = 'danger' | 'success' | 'warning' | 'info' | 'accent' | 'neutral';
interface ActionMeta { label: string; tone: Tone; icon: string; }
interface Toast { notification: DashboardNotification; timer: ReturnType<typeof setTimeout>; }

const ICONS = {
  plug: 'M9 2v6m6-6v6M6 8h12v3a6 6 0 0 1-12 0V8Zm6 9v5',
  warning: 'M12 9v4m0 4h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z',
  message: 'M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v10Z',
  download: 'M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4m4-5 5 5 5-5m-5 5V3',
  bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9m-4.3 13a2 2 0 0 1-3.4 0'
};
const ACTIONS: Record<string, ActionMeta> = {
  ChargePointOffline: { label: 'Charge point offline', tone: 'danger', icon: ICONS.plug },
  ChargePointOnline: { label: 'Charge point back online', tone: 'success', icon: ICONS.plug },
  ChargePointConnected: { label: 'Charge point connected', tone: 'success', icon: ICONS.plug },
  ChargePointDisconnected: { label: 'Charge point disconnected', tone: 'warning', icon: ICONS.plug },
  SystemReportCreated: { label: 'System report', tone: 'warning', icon: ICONS.warning },
  ReportCreated: { label: 'User report', tone: 'info', icon: ICONS.message },
  UserInfoDownloadRequested: { label: 'Data request', tone: 'accent', icon: ICONS.download }
};
const TOAST_MS = 6000;
const URGENT_TOAST_MS = 12000;
const MAX_TOASTS = 3;
// Only routes of this SPA are navigable; mobile-app paths stored for customers are not.
const NAVIGABLE = /^\/(dashboard|partner-dashboard|my-dashboard)(\/|$)/;

@Component({
  selector: 'app-navbar-notifications',
  templateUrl: './navbar-notifications.component.html',
  styleUrls: ['./navbar-notifications.component.sass']
})
export class NavbarNotificationsComponent implements OnInit, OnDestroy {
  @ViewChild('toggle') toggle?: ElementRef<HTMLButtonElement>;
  state: NotificationState = { items: [], unreadCount: 0, loading: false, error: false };
  open = false;
  filter: 'all' | 'unread' = 'all';
  toasts: Toast[] = [];
  now = Date.now();
  private clock?: ReturnType<typeof setInterval>;
  private subscriptions = new Subscription();

  constructor(private notifications: NotificationService, private router: Router, private confirm: ConfirmService,
    private host: ElementRef<HTMLElement>, private zone: NgZone) {}

  ngOnInit(): void {
    this.subscriptions.add(this.notifications.state$.subscribe(state => this.state = state));
    this.subscriptions.add(this.notifications.received$.subscribe(notification => this.showToast(notification)));
    this.notifications.connect();
    this.clock = setInterval(() => this.now = Date.now(), 60000);
    // Outside Angular: a document-wide listener would otherwise run change detection on every click in the app.
    this.zone.runOutsideAngular(() => document.addEventListener('click', this.onDocumentClick));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    clearInterval(this.clock);
    this.toasts.forEach(toast => clearTimeout(toast.timer));
    this.notifications.disconnect();
    document.removeEventListener('click', this.onDocumentClick);
  }

  get visibleItems(): DashboardNotification[] {
    return this.filter === 'unread' ? this.state.items.filter(item => !item.read) : this.state.items;
  }

  get badge(): string { return this.state.unreadCount > 99 ? '99+' : String(this.state.unreadCount); }

  meta(notification: DashboardNotification): ActionMeta {
    return ACTIONS[notification.action || ''] || { label: notification.type || 'Notification', tone: 'neutral', icon: ICONS.bell };
  }

  timeAgo(value: string): string {
    const minutes = Math.floor((this.now - new Date(value).getTime()) / 60000);
    if (minutes < 1) return 'now';
    if (minutes < 60) return minutes + 'm';
    if (minutes < 1440) return Math.floor(minutes / 60) + 'h';
    if (minutes < 10080) return Math.floor(minutes / 1440) + 'd';
    return new Date(value).toLocaleDateString();
  }

  togglePanel(): void {
    this.open = !this.open;
    if (this.open) { this.now = Date.now(); if (this.state.error) this.notifications.reload(); }
  }

  close(restoreFocus = false): void {
    if (!this.open) return;
    this.open = false;
    if (restoreFocus) this.toggle?.nativeElement.focus();
  }

  private onDocumentClick = (event: MouseEvent): void => {
    // composedPath is fixed at dispatch, so clicks on elements removed by the click itself
    // (remove, retry) still count as inside.
    if (this.open && !event.composedPath().includes(this.host.nativeElement)) this.zone.run(() => this.close());
  };

  openNotification(notification: DashboardNotification): void {
    this.notifications.markAsRead(notification);
    this.dismissToast(notification.id);
    this.close();
    if (this.isNavigable(notification)) this.router.navigateByUrl(notification.url);
  }

  isNavigable(notification: DashboardNotification): boolean { return NAVIGABLE.test(notification.url || ''); }

  markAllAsRead(): void { this.notifications.markAllAsRead(); }

  remove(notification: DashboardNotification, event: Event): void {
    event.stopPropagation();
    this.notifications.remove(notification);
  }

  requestClearAll(): void {
    this.confirm.requestConfirmation(
      'Clear all notifications?',
      'All of your notifications will be removed. This cannot be undone.',
      () => this.notifications.removeAll(),
      { confirmLabel: 'Clear all', tone: 'danger' }
    );
  }

  retry(): void { this.notifications.reload(); }

  dismissToast(id: number): void {
    const toast = this.toasts.find(item => item.notification.id === id);
    if (!toast) return;
    clearTimeout(toast.timer);
    this.toasts = this.toasts.filter(item => item !== toast);
  }

  private showToast(notification: DashboardNotification): void {
    if (this.open) return; // Already visible at the top of the open list.
    const timer = setTimeout(() => this.dismissToast(notification.id), notification.urgent ? URGENT_TOAST_MS : TOAST_MS);
    this.toasts = [{ notification, timer }, ...this.toasts];
    this.toasts.slice(MAX_TOASTS).forEach(toast => this.dismissToast(toast.notification.id));
  }
}
