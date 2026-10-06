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
    storage.startSession({ token: 'old-access' });
  });

  afterEach(() => { http.verify(); storage.clear(); });

  it('sends the refresh cookie, never a token in the body or in storage', () => {
    service.refresh().subscribe();
    const request = http.expectOne(r => r.url.endsWith('/Refresh'));
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.headers.get('X-Token-Transport')).toBe('cookie');
    expect(request.request.body).toEqual({});
    request.flush({ token: 'new-access' });
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('shares one refresh between concurrent callers and keeps the access token in memory', () => {
    const tokens: string[] = [];
    service.refresh().subscribe(t => tokens.push(t));
    service.refresh().subscribe(t => tokens.push(t));
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({ token: 'new-access' });
    expect(tokens).toEqual(['new-access', 'new-access']);
    expect(storage.accessToken).toBe('new-access');
    expect(service.isRefreshing).toBeFalse();
  });

  it('serializes tabs through the Web Lock', fakeAsync(() => {
    let acquire!: () => void;
    const locks = { request: (_name: string, callback: () => Promise<string>) =>
      new Promise<string>((resolve, reject) => { acquire = () => { callback().then(resolve, reject); }; })
    } as unknown as LockManager;
    const tab = new TokenRefreshService(TestBed.inject(HttpClient), storage, locks);
    let token: string | undefined;
    tab.refresh().subscribe(value => token = value);
    http.expectNone(r => r.url.endsWith('/Refresh'));
    acquire();
    flushMicrotasks();
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({ token: 'locked-access' });
    flushMicrotasks();
    expect(token).toBe('locked-access');
  }));

  it('does not try to refresh without a known session', () => {
    storage.clear();
    let failed = false;
    service.refresh().subscribe({ error: () => failed = true });
    http.expectNone(r => r.url.endsWith('/Refresh'));
    expect(failed).toBeTrue();
  });

  it('preserves the session on server failure and permits a later retry', () => {
    service.refresh().subscribe({ error: () => {} });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(storage.hasRefreshToken()).toBeTrue();
    service.refresh().subscribe();
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({ token: 'new-access' });
    expect(storage.accessToken).toBe('new-access');
  });

  it('clears a rejected session', () => {
    service.refresh().subscribe({ error: () => {} });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(storage.accessToken).toBeNull();
    expect(storage.hasRefreshToken()).toBeFalse();
  });

  it('does not resurrect a session when refresh finishes after logout', () => {
    service.refresh().subscribe({ error: () => {} });
    const refresh = http.expectOne(r => r.url.endsWith('/Refresh'));
    service.revoke().subscribe();
    const logout = http.expectOne(r => r.url.endsWith('/Logout'));
    expect(logout.request.withCredentials).toBeTrue();
    logout.flush(null);
    refresh.flush({ token: 'new-access' });
    expect(storage.accessToken).toBeNull();
    expect(storage.hasRefreshToken()).toBeFalse();
  });

  it('does not clear a newer login when the previous refresh is rejected', () => {
    service.refresh().subscribe({ error: () => {} });
    storage.startSession({ token: 'other-access' });
    http.expectOne(r => r.url.endsWith('/Refresh')).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(storage.accessToken).toBe('other-access');
  });
});
