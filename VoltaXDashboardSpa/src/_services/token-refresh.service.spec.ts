import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { fakeAsync, flushMicrotasks, TestBed } from '@angular/core/testing';
import { REFRESH_LOCKS, TokenRefreshService } from './token-refresh.service';
import { HttpClient } from '@angular/common/http';
import { TokenStorageService } from './token-storage.service';

describe('TokenRefreshService', () => {
  let service: TokenRefreshService;
  let storage: TokenStorageService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [{ provide: REFRESH_LOCKS, useValue: null }] });
    service = TestBed.inject(TokenRefreshService);
    storage = TestBed.inject(TokenStorageService);
    http = TestBed.inject(HttpTestingController);
    storage.clear();
    storage.store({ token: 'old-access', refreshToken: 'old-refresh' });
  });

  afterEach(() => { http.verify(); storage.clear(); });

  it('uses a successor already rotated by another tab while waiting for the lock', fakeAsync(() => {
    let acquire!: () => Promise<string>;
    const locks = { request: (_name: string, callback: () => Promise<string>) =>
      new Promise<string>(resolve => { acquire = () => callback().then(resolve) as Promise<any>; })
    } as unknown as LockManager;
    const tab = new TokenRefreshService(TestBed.inject(HttpClient), storage, locks);
    let token: string | undefined;
    tab.refresh().subscribe(value => token = value);
    storage.store({ token: 'other-tab-access', refreshToken: 'other-tab-refresh' });
    acquire();
    flushMicrotasks();
    http.expectNone(r => r.url.endsWith('/Refresh'));
    expect(token).toBe('other-tab-access');
  }));

  it('shares a rotation between concurrent callers and stores the successor', () => {
    const tokens: string[] = [];
    service.refresh().subscribe(t => tokens.push(t));
    service.refresh().subscribe(t => tokens.push(t));
    const request = http.expectOne(r => r.url.endsWith('/Refresh'));
    expect(request.request.body.refreshToken).toBe('old-refresh');
    request.flush({ token: 'new-access', refreshToken: 'new-refresh' });
    expect(tokens).toEqual(['new-access', 'new-access']);
    expect(storage.refreshToken).toBe('new-refresh');
    expect(service.isRefreshing).toBeFalse();
  });

  it('preserves the session on server failure and permits a later retry', () => {
    service.refresh().subscribe({ error: () => {} });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(storage.refreshToken).toBe('old-refresh');
    service.refresh().subscribe();
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({ token: 'new-access', refreshToken: 'new-refresh' });
  });

  it('clears a rejected session', () => {
    service.refresh().subscribe({ error: () => {} });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(storage.accessToken).toBeNull();
    expect(storage.refreshToken).toBeNull();
  });

  it('does not resurrect a session when refresh finishes after logout', () => {
    service.refresh().subscribe({ error: () => {} });
    const refresh = http.expectOne(r => r.url.endsWith('/Refresh'));
    service.revoke().subscribe();
    expect(storage.accessToken).toBeNull();
    http.expectOne(r => r.url.endsWith('/Logout')).flush(null);
    refresh.flush({ token: 'new-access', refreshToken: 'new-refresh' });
    const revoke = http.expectOne(r => r.url.endsWith('/Logout'));
    expect(revoke.request.body.refreshToken).toBe('new-refresh');
    revoke.flush(null);
    expect(storage.refreshToken).toBeNull();
  });

  it('does not clear a newer login when the previous refresh is rejected', () => {
    service.refresh().subscribe({ error: () => {} });
    storage.store({ token: 'other-access', refreshToken: 'other-refresh' });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(storage.refreshToken).toBe('other-refresh');
  });
});
