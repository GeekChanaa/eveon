import { HttpErrorResponse, HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';
import { TokenStorageService } from 'src/_services/token-storage.service';
import { TokenRefreshService } from 'src/_services/token-refresh.service';
import { TokenInterceptor } from './token.interceptor';

describe('TokenInterceptor', () => {
  let storage: TokenStorageService;
  let refresh: jasmine.SpyObj<TokenRefreshService>;
  let router: jasmine.SpyObj<Router>;
  let interceptor: TokenInterceptor;
  const api = environment.apiUrl + '/api/';

  beforeEach(() => {
    storage = new TokenStorageService();
    storage.startSession({ token: 'access' });
    spyOn(storage, 'isAccessTokenExpired').and.returnValue(false);
    spyOn(storage, 'isAccessTokenAboutToExpire').and.returnValue(false);
    refresh = jasmine.createSpyObj('TokenRefreshService', ['refresh']);
    refresh.refresh.and.returnValue(of('renewed'));
    router = jasmine.createSpyObj('Router', ['navigateByUrl'], { url: '/dashboard' });
    interceptor = new TokenInterceptor(storage, refresh, router);
  });
  afterEach(() => storage.clear());

  it('authenticates the Google link ticket but leaves Google login anonymous', () => {
    const handler: HttpHandler = { handle: request => {
      expect(request.headers.get('Authorization')).toBe('Bearer access');
      return of(new HttpResponse());
    }};
    interceptor.intercept(new HttpRequest('POST', api + 'auth/google/link-ticket', {}), handler).subscribe();
    interceptor.intercept(new HttpRequest('POST', api + 'auth/google', {}), {
      handle: request => {
        expect(request.headers.has('Authorization')).toBeFalse();
        return of(new HttpResponse());
      }
    }).subscribe();
  });

  it('does not send credentials to an unrelated origin', () => {
    interceptor.intercept(new HttpRequest('GET', 'https://example.invalid/api/users'), {
      handle: request => {
        expect(request.headers.has('Authorization')).toBeFalse();
        return of(new HttpResponse());
      }
    }).subscribe();
    expect(refresh.refresh).not.toHaveBeenCalled();
  });

  it('authenticates OCPP requests to our API origin', () => {
    const requestUrl = environment.apiUrl + '/ocpp/EVDriver/RequestStartTransaction/VOLTAX-008';

    interceptor.intercept(new HttpRequest('POST', requestUrl, {}), {
      handle: request => {
        expect(request.headers.get('Authorization')).toBe('Bearer access');
        return of(new HttpResponse());
      }
    }).subscribe();
  });

  it('preserves the session when a business request fails after proactive refresh', () => {
    (storage.isAccessTokenExpired as jasmine.Spy).and.returnValue(true);
    interceptor.intercept(new HttpRequest('GET', api + 'users'), {
      handle: () => throwError(() => new HttpErrorResponse({ status: 500 }))
    }).subscribe({ error: error => expect(error.status).toBe(500) });
    expect(storage.hasRefreshToken()).toBeTrue();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('sends the refresh cookie on session endpoints only', () => {
    interceptor.intercept(new HttpRequest('POST', api + 'auth/Refresh', {}), {
      handle: request => {
        expect(request.withCredentials).toBeTrue();
        expect(request.headers.get('X-Token-Transport')).toBe('cookie');
        expect(request.headers.has('Authorization')).toBeFalse();
        return of(new HttpResponse());
      }
    }).subscribe();
    interceptor.intercept(new HttpRequest('GET', api + 'users'), {
      handle: request => {
        expect(request.withCredentials).toBeFalse();
        return of(new HttpResponse());
      }
    }).subscribe();
  });

  it('retries a 401 only once', () => {
    const handle = jasmine.createSpy('handle').and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401 })));
    interceptor.intercept(new HttpRequest('GET', api + 'users'), { handle }).subscribe({ error: () => {} });
    expect(handle).toHaveBeenCalledTimes(2);
    expect(refresh.refresh).toHaveBeenCalledTimes(1);
  });
});
