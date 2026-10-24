import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { HttpTestingController, TestRequest } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Alarm, AlarmPage } from 'src/_models/alarm';
import { AccessService } from 'src/_services/access.service';
import { HUB_CONNECTION_FACTORY, HUB_IDLE_CLOSE_MS } from 'src/_services/realtime-hub.service';
import { ToastService } from 'src/_services/toast.service';
import { environment } from 'src/environments/environment';
import { FakeHubConnection, fakeHubFactory } from 'src/testing/fake-hub-connection';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { AlarmManagementPageComponent } from './alarm-management-page.component';

const API = `${environment.apiUrl}/api/Alarm`;

function alarm(overrides: Partial<Alarm> = {}): Alarm {
  return {
    id: 1, type: 'ConnectorFault', code: 'GroundFailure', severity: 'Critical', status: 'Open', description: 'Connector 1 faulted (GroundFailure)',
    details: 'RCD tripped', chargePointID: 5, chargePointIdentity: 'CP-5', chargingStationID: 2, stationName: 'Marina', evseId: 1, connectorNumber: 1,
    connectorID: 9, firstSeenAt: '2026-10-20T09:00:00Z', lastSeenAt: '2026-10-20T09:05:00Z', count: 2, acknowledgedAt: null, acknowledgedBy: null,
    assignedToID: null, assignedTo: null, assignedAt: null, resolvedAt: null, resolvedBy: null, resolutionComment: null, clearedAt: null, ...overrides
  };
}

function pageOf(...items: Alarm[]): AlarmPage {
  return { items, totalCount: items.length, page: 1, pageSize: 25 };
}

describe('AlarmManagementPageComponent', () => {
  let fixture: ComponentFixture<AlarmManagementPageComponent>;
  let component: AlarmManagementPageComponent;
  let http: HttpTestingController;
  let connections: FakeHubConnection[];
  let permissions: Set<string>;
  let queryParams: Record<string, string>;
  let toast: jasmine.SpyObj<ToastService>;

  function create(): void {
    TestBed.configureTestingModule({
      imports: [VxTestingModule, AlarmManagementPageComponent],
      providers: [
        { provide: HUB_CONNECTION_FACTORY, useValue: fakeHubFactory(connections) },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
        { provide: AccessService, useValue: { can: (p: string) => permissions.has(p), canUrl: () => true, isAdmin: false, current: { userId: 42 } } },
        { provide: ToastService, useValue: toast }
      ]
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AlarmManagementPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  /** Answers the requests every page load makes. */
  function answerLoad(list: AlarmPage = pageOf(alarm(), alarm({ id: 2, severity: 'Minor', code: 'ReaderFailure', description: 'Reader failure' }))): TestRequest {
    const listRequest = http.expectOne(r => r.url === API);
    listRequest.flush(list);
    http.expectOne(`${API}/summary`).flush({ active: 2, critical: 1, major: 0, minor: 1, warning: 0, unassigned: 2, assignedToMe: 0 });
    http.expectOne(r => r.url.endsWith('GetChargingStationsForSelect/')).flush([{ id: 2, name: 'Marina' }]);
    fixture.detectChanges();
    return listRequest;
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const rows = () => fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;

  beforeEach(() => {
    connections = [];
    permissions = new Set(['ViewAlarms', 'EditAlarms']);
    queryParams = {};
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'warning', 'apiError']);
  });

  afterEach(() => http.verify());

  it('lists the active alarms, latest first, with the summary and the live feed joined', fakeAsync(() => {
    create();
    const request = answerLoad();
    expect(request.request.params.get('status')).toBe('active');
    expect(request.request.params.get('page')).toBe('1');
    expect(rows().length).toBe(2);
    expect(text()).toContain('Connector 1 faulted (GroundFailure)');
    expect(text()).toContain('CP-5');
    flushMicrotasks();
    expect(connections[0].joined()).toEqual([['#alarms']]);
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('sends the filters and resets them', fakeAsync(() => {
    create();
    answerLoad();
    component.filter.severity = 'Critical';
    component.filter.type = 'Offline';
    component.filter.chargingStationID = 2;
    component.filter.assigned = 'me';
    component.filter.search = ' CP-5 ';
    component.filter.from = '2026-10-01';
    component.search();
    const request = http.expectOne(r => r.url === API);
    const params = request.request.params;
    expect(params.get('severity')).toBe('Critical');
    expect(params.get('type')).toBe('Offline');
    expect(params.get('chargingStationID')).toBe('2');
    expect(params.get('assignedToMe')).toBe('true');
    expect(params.get('search')).toBe('CP-5');
    expect(new Date(params.get('from')!).getDate()).toBe(1);
    request.flush(pageOf());
    fixture.detectChanges();
    expect(text()).toContain('alarms.empty');

    component.reset();
    expect(http.expectOne(r => r.url === API).request.params.get('severity')).toBeNull();
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('shows an error with a retry when the list cannot be loaded', fakeAsync(() => {
    create();
    http.expectOne(r => r.url === API).flush('down', { status: 500, statusText: 'Server Error' });
    http.expectOne(`${API}/summary`).flush('down', { status: 500, statusText: 'Server Error' });
    http.expectOne(r => r.url.endsWith('GetChargingStationsForSelect/')).flush('no', { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();
    expect(component.loading).toBeFalse();
    expect(text()).toContain('alarms.loadError');
    expect(fixture.nativeElement.querySelector('#alarm-station')).toBeNull();

    (fixture.nativeElement.querySelector('.vx-alert--error button') as HTMLButtonElement).click();
    http.expectOne(r => r.url === API).flush(pageOf(alarm()));
    fixture.detectChanges();
    expect(text()).not.toContain('alarms.loadError');
    expect(rows().length).toBe(1);
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('opens an alarm, acknowledges it and assigns it', fakeAsync(() => {
    create();
    answerLoad();
    rows()[0].click();
    http.expectOne(`${API}/1`).flush(alarm());
    http.expectOne(`${API}/assignees`).flush([{ id: 42, name: 'Me', email: 'me@eveon.test' }, { id: 7, name: 'Tech', email: 'tech@eveon.test' }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alarms__drawer')).not.toBeNull();

    component.acknowledge(component.selected!);
    const ack = http.expectOne(`${API}/1/acknowledge`);
    expect(ack.request.method).toBe('POST');
    ack.flush(alarm({ status: 'Acknowledged', acknowledgedBy: 'Me', acknowledgedAt: '2026-10-20T09:06:00Z' }));
    http.expectOne(`${API}/summary`).flush({ active: 2, critical: 1, major: 0, minor: 1, warning: 0, unassigned: 2, assignedToMe: 0 });
    expect(toast.success).toHaveBeenCalledWith('alarms.toast.acknowledged');
    expect(component.page!.items[0].status).toBe('Acknowledged');

    component.assignTo = 7;
    component.assign(component.selected!);
    const assign = http.expectOne(`${API}/1/assign`);
    expect(assign.request.body).toEqual({ assignedToID: 7 });
    assign.flush(alarm({ status: 'Acknowledged', assignedToID: 7, assignedTo: 'Tech' }));
    http.expectOne(`${API}/summary`).flush({ active: 2, critical: 1, major: 0, minor: 1, warning: 0, unassigned: 1, assignedToMe: 0 });
    expect(toast.success).toHaveBeenCalledWith('alarms.toast.assigned');
    fixture.detectChanges();
    expect(text()).toContain('Tech');
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('needs a comment to resolve an alarm and sends it', fakeAsync(() => {
    create();
    answerLoad();
    component.open(1);
    http.expectOne(`${API}/1`).flush(alarm());
    http.expectOne(`${API}/assignees`).flush([]);
    fixture.detectChanges();

    component.resolve(component.selected!);
    fixture.detectChanges();
    http.expectNone(`${API}/1/resolve`);
    expect(text()).toContain('alarms.detail.commentRequired');

    component.comment = '  Replaced the RCD  ';
    component.resolve(component.selected!);
    const resolve = http.expectOne(`${API}/1/resolve`);
    expect(resolve.request.body).toEqual({ comment: 'Replaced the RCD' });
    resolve.flush(alarm({ status: 'Resolved', resolutionComment: 'Replaced the RCD', resolvedBy: 'Me' }));
    http.expectOne(`${API}/summary`).flush({ active: 1, critical: 0, major: 0, minor: 1, warning: 0, unassigned: 1, assignedToMe: 0 });
    fixture.detectChanges();
    expect(toast.success).toHaveBeenCalledWith('alarms.toast.resolved');
    expect(fixture.nativeElement.querySelector('#alarm-comment')).toBeNull();
    expect(text()).toContain('alarms.detail.resolvedHint');
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('shows what an alarm closed meanwhile became when an action is refused', fakeAsync(() => {
    create();
    answerLoad();
    component.open(1);
    http.expectOne(`${API}/1`).flush(alarm());
    http.expectOne(`${API}/assignees`).flush([]);
    component.acknowledge(component.selected!);
    http.expectOne(`${API}/1/acknowledge`).flush({ error: 'The alarm is already closed.' }, { status: 409, statusText: 'Conflict' });
    expect(toast.apiError).toHaveBeenCalled();
    http.expectOne(`${API}/1`).flush(alarm({ status: 'Cleared', clearedAt: '2026-10-20T09:10:00Z' }));
    expect(component.selected!.status).toBe('Cleared');
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('reloads once per burst of live changes and keeps the open alarm current', fakeAsync(() => {
    create();
    answerLoad();
    component.open(1);
    http.expectOne(`${API}/1`).flush(alarm());
    http.expectOne(`${API}/assignees`).flush([]);
    flushMicrotasks();

    connections[0].emit('AlarmChanged', { change: 'Updated', alarm: alarm({ count: 3 }) });
    connections[0].emit('AlarmChanged', { change: 'Raised', alarm: alarm({ id: 3, code: 'Offline', type: 'Offline' }) });
    expect(component.selected!.count).toBe(3);
    tick(800);
    http.expectOne(r => r.url === API).flush(pageOf(alarm({ count: 3 })));
    http.expectOne(`${API}/summary`).flush({ active: 1, critical: 1, major: 0, minor: 0, warning: 0, unassigned: 1, assignedToMe: 0 });
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('opens the alarm of the link and is read only without the edit permission', fakeAsync(() => {
    queryParams = { alarm: '1' };
    permissions = new Set(['ViewAlarms']);
    create();
    http.expectOne(`${API}/1`).flush(alarm());
    answerLoad();
    expect(fixture.nativeElement.querySelector('.alarms__drawer')).not.toBeNull();
    expect(text()).toContain('alarms.detail.readOnly');
    expect(fixture.nativeElement.querySelector('#alarm-comment')).toBeNull();
    http.expectNone(`${API}/assignees`);
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));

  it('exports the filtered alarms as a file', fakeAsync(() => {
    create();
    answerLoad();
    component.filter.severity = 'Major';
    component.export();
    const request = http.expectOne(r => r.url === `${API}/export`);
    expect(request.request.params.get('severity')).toBe('Major');
    expect(request.request.params.get('page')).toBeNull();
    request.flush(new Blob(['boom']), { status: 500, statusText: 'Server Error' });
    expect(component.exporting).toBeFalse();
    expect(toast.apiError).toHaveBeenCalled();
    fixture.destroy();
    tick(HUB_IDLE_CLOSE_MS);
  }));
});
