import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { of } from 'rxjs';
import { MessageLogService } from 'src/_services/message-log.service';
import { HUB_CONNECTION_FACTORY, HUB_IDLE_CLOSE_MS } from 'src/_services/realtime-hub.service';
import { FakeHubConnection, fakeHubFactory } from 'src/testing/fake-hub-connection';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { ConnectorRealtimeLogsComponent } from './connector-realtime-logs.component';

describe('ConnectorRealtimeLogsComponent', () => {
  let fixture: ComponentFixture<ConnectorRealtimeLogsComponent>;
  let component: ConnectorRealtimeLogsComponent;
  let logs: jasmine.SpyObj<MessageLogService>;
  let connections: FakeHubConnection[];

  beforeEach(async () => {
    connections = [];
    logs = jasmine.createSpyObj<MessageLogService>('MessageLogService', ['getChargePointMessageLogs']);
    logs.getChargePointMessageLogs.and.returnValue(of({ result: [{ id: 1, message: 'Heartbeat', logTime: '2026-10-20T09:00:00' }], pagination: {} } as any));
    await configureVxTest([ConnectorRealtimeLogsComponent], {
      providers: [
        { provide: MessageLogService, useValue: logs },
        { provide: HUB_CONNECTION_FACTORY, useValue: fakeHubFactory(connections) }
      ]
    });
    fixture = TestBed.createComponent(ConnectorRealtimeLogsComponent);
    component = fixture.componentInstance;
    component.chargePoint = { chargePointId: 'CP-1' };
    fixture.detectChanges();
  });

  it('loads the first page with the times read as UTC', () => {
    expect(logs.getChargePointMessageLogs).toHaveBeenCalledWith('CP-1', 1, 20, jasmine.any(Object));
    expect(component.messageLogs[0].logTime).toBe('2026-10-20T09:00:00Z');
  });

  it('reloads the first page once per burst of live messages while live mode is on', fakeAsync(() => {
    component.realtimeOnToggle(true);
    flushMicrotasks();
    const calls = logs.getChargePointMessageLogs.calls.count();
    expect(connections[0].joined()).toEqual([['CP-1']]);

    connections[0].emit('SentMessage', '[2,"1","Heartbeat",{}]');
    connections[0].emit('ReceiveMessage', '[3,"1",{}]');
    tick(1000);
    expect(logs.getChargePointMessageLogs.calls.count()).toBe(calls + 1);

    component.realtimeOnToggle(false);
    expect(connections[0].left()).toEqual([['CP-1']]);
    connections[0].emit('SentMessage', '[2,"2","Heartbeat",{}]');
    tick(1000 + HUB_IDLE_CLOSE_MS);
    expect(logs.getChargePointMessageLogs.calls.count()).toBe(calls + 1);
  }));

  it('refuses a period that ends before it starts', () => {
    component.filters.from = '2026-10-20T10:00';
    component.filters.to = '2026-10-20T09:00';
    const calls = logs.getChargePointMessageLogs.calls.count();
    component.applyFilters();
    expect(component.rangeError).toBeTruthy();
    expect(logs.getChargePointMessageLogs.calls.count()).toBe(calls);
  });
});
