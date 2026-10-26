import { TestBed } from '@angular/core/testing';
import { DashboardNotification } from 'src/_models/notification';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { BrowserNotificationService } from './browser-notification.service';

describe('BrowserNotificationService', () => {
  let service: BrowserNotificationService;
  let created: { title: string; options: NotificationOptions }[];
  let permission: NotificationPermission;
  const original = (window as any).Notification;

  const notification = (action: string): DashboardNotification => ({
    id: 1, action, description: 'Charge point CP1 (Station) was added.', url: '/dashboard/charging-points/1',
    urgent: true, read: false, createdAt: new Date().toISOString()
  });

  beforeEach(() => {
    created = [];
    permission = 'granted';
    const fake: any = function (this: any, title: string, options: NotificationOptions) { created.push({ title, options }); this.close = () => {}; };
    Object.defineProperty(fake, 'permission', { get: () => permission });
    fake.requestPermission = () => Promise.resolve(permission);
    (window as any).Notification = fake;
    TestBed.configureTestingModule({ imports: [VxTestingModule] });
    service = TestBed.inject(BrowserNotificationService);
    spyOn(document, 'hasFocus').and.returnValue(false);
  });

  afterEach(() => (window as any).Notification = original);

  it('shows charge points needing configuration while the tab is in the background', () => {
    service.show(notification('ChargePointAdded'));
    service.show(notification('ChargePointAwaitingProvisioning'));
    expect(created.length).toBe(2);
    expect(created[0].options.body).toContain('CP1');
  });

  it('ignores other notifications', () => {
    service.show(notification('ChargePointOffline'));
    expect(created.length).toBe(0);
  });

  it('does nothing without the permission', () => {
    permission = 'denied';
    service.show(notification('ChargePointAdded'));
    expect(created.length).toBe(0);
  });
});
