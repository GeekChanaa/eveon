import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { AlarmFilter } from 'src/_models/alarm';
import { environment } from 'src/environments/environment';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { AlarmService } from './alarm.service';

describe('AlarmService', () => {
  const base: AlarmFilter = { status: 'active', severity: '', type: '', chargingStationID: null, assigned: '', from: null, to: null, search: '', sort: '', page: 2, pageSize: 25 };
  let service: AlarmService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [VxTestingModule] });
    service = TestBed.inject(AlarmService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('leaves empty filters out of the query string', () => {
    const params = AlarmService.params(base);
    expect(params.keys().sort()).toEqual(['page', 'pageSize', 'status']);
  });

  it('maps the assignee choice and whole days', () => {
    const params = AlarmService.params({ ...base, assigned: 'none', from: '2026-10-01', to: '2026-10-02', sort: 'severity' }, false);
    expect(params.get('unassigned')).toBe('true');
    expect(params.has('assignedToMe')).toBeFalse();
    expect(params.has('page')).toBeFalse();
    expect(new Date(params.get('to')!).getTime() - new Date(params.get('from')!).getTime()).toBe(2 * 24 * 3600 * 1000 - 1);
    expect(params.get('sort')).toBe('severity');
  });

  it('posts the operator actions', () => {
    service.acknowledge(4).subscribe();
    service.assign(4, null).subscribe();
    service.resolve(4, 'Fixed').subscribe();
    const url = `${environment.apiUrl}/api/Alarm/4`;
    expect(http.expectOne(`${url}/acknowledge`).request.method).toBe('POST');
    expect(http.expectOne(`${url}/assign`).request.body).toEqual({ assignedToID: null });
    expect(http.expectOne(`${url}/resolve`).request.body).toEqual({ comment: 'Fixed' });
  });
});
