import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';
import { DashboardNotification, Notification } from 'src/_models/notification';
import { AbstractService } from './abstract-service';
import { RealtimeHubService } from './realtime-hub.service';

export interface NotificationState {
  items: DashboardNotification[];
  unreadCount: number;
  loading: boolean;
  error: boolean;
}

const EMPTY_STATE: NotificationState = { items: [], unreadCount: 0, loading: false, error: false };
const PAGE_SIZE = 20;

/**
 * Owns the signed-in user's notification list and the realtime connection that keeps it current.
 * The server pushes "NotificationReceived" on /notificationHub; any missed while disconnected are
 * picked up by reloading the list on reconnect.
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationService extends AbstractService<Notification> {
  constructor(protected http: HttpClient, private realtime: RealtimeHubService) {
    super(http, environment.apiUrl + "/api/notification/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl + "/api/notification/";

  private live?: Subscription;
  private readonly state = new BehaviorSubject<NotificationState>(EMPTY_STATE);
  private readonly received = new Subject<DashboardNotification>();
  readonly state$: Observable<NotificationState> = this.state.asObservable();
  /** Fires once per notification pushed in realtime (not for reloaded ones). */
  readonly received$: Observable<DashboardNotification> = this.received.asObservable();

  /** Loads the list and follows the pushes (shared hub connection, reconnected automatically). */
  connect(): void {
    if (this.live) return;
    this.reload();
    this.live = new Subscription();
    this.live.add(this.realtime.on<DashboardNotification>('notification', 'NotificationReceived').subscribe(notification => {
      const current = this.state.value;
      if (current.items.some(item => item.id === notification.id)) return;
      this.patch({ items: [notification, ...current.items].slice(0, PAGE_SIZE), unreadCount: current.unreadCount + 1 });
      this.received.next(notification);
    }));
    // Notifications pushed while disconnected are picked up by reloading the list.
    this.live.add(this.realtime.reconnected('notification').subscribe(() => this.reload()));
  }

  disconnect(): void {
    this.live?.unsubscribe();
    this.live = undefined;
    this.state.next(EMPTY_STATE);
  }

  reload(): void {
    this.patch({ loading: true, error: false });
    this._http.get<{ unreadCount: number; items: DashboardNotification[] }>(this.baseUrl + "GetMyNotifications", { params: { take: PAGE_SIZE } }).subscribe({
      next: result => this.state.next({ items: result.items, unreadCount: result.unreadCount, loading: false, error: false }),
      error: () => this.patch({ loading: false, error: true })
    });
  }

  markAsRead(notification: DashboardNotification): void {
    if (notification.read) return;
    const previous = this.state.value;
    this.patch({
      items: previous.items.map(item => item.id === notification.id ? { ...item, read: true } : item),
      unreadCount: Math.max(0, previous.unreadCount - 1)
    });
    this._http.put(this.baseUrl + "MarkAsRead/" + notification.id, {}).subscribe({ error: () => this.state.next(previous) });
  }

  markAllAsRead(): void {
    const previous = this.state.value;
    this.patch({ items: previous.items.map(item => ({ ...item, read: true })), unreadCount: 0 });
    this._http.put(this.baseUrl + "MarkAllAsRead", {}).subscribe({ error: () => this.state.next(previous) });
  }

  remove(notification: DashboardNotification): void {
    const previous = this.state.value;
    this.patch({
      items: previous.items.filter(item => item.id !== notification.id),
      unreadCount: notification.read ? previous.unreadCount : Math.max(0, previous.unreadCount - 1)
    });
    // Reload so the list refills from older notifications.
    this._http.delete(this.baseUrl + "DeleteMyNotification/" + notification.id).subscribe({
      next: () => this.reload(),
      error: () => this.state.next(previous)
    });
  }

  removeAll(): void {
    const previous = this.state.value;
    this.patch({ items: [], unreadCount: 0 });
    this._http.delete(this.baseUrl + "DeleteAllMyNotifications").subscribe({ error: () => this.state.next(previous) });
  }

  private patch(changes: Partial<NotificationState>): void {
    this.state.next({ ...this.state.value, ...changes });
  }
}
