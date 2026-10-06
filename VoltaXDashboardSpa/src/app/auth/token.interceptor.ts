import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { Observable, throwError } from "rxjs";
import { catchError, switchMap } from "rxjs/operators";
import { TokenRefreshService } from "src/_services/token-refresh.service";
import { TokenStorageService } from "src/_services/token-storage.service";
import { environment } from "src/environments/environment";

/**
 * Attaches the access token to every call and keeps the session alive.
 *
 * Two moments trigger a refresh:
 *  - before a call, when the access token is already expired (or about to be), so the
 *    request is not spent on a guaranteed 401;
 *  - after a call that came back 401, which covers a token revoked server side.
 *
 * Endpoints under /api/auth that do not need a token are left alone — refreshing while
 * refreshing, or while logging in, would loop.
 */
@Injectable()
export class TokenInterceptor implements HttpInterceptor {

    /** Calls that must never carry a token or trigger a refresh. */
    private static readonly ANONYMOUS_PATHS = [
        '/api/auth/login',
        '/api/auth/register',
        '/api/auth/refresh',
        '/api/auth/logout',
        '/api/auth/checktoken',
        '/api/auth/resetpassword',
        '/api/auth/request-password-reset',
        '/api/auth/verify-2fa',
        '/api/auth/phone/request',
        '/api/auth/phone/verify',
        '/api/auth/google',
        '/api/auth/google-login',
        '/api/auth/google-callback',
        '/api/auth/verifyresetpasswordcodeformobile',
        '/api/auth/resetpasswordformobile',
        '/api/partnerauth/login',
        '/api/partnerauth/google',
        '/api/partnerauth/google-login',
        '/api/partnerauth/request-reset',
        '/api/partnerauth/reset'
    ];

    constructor(
        private _tokenStorage: TokenStorageService,
        private _tokenRefresh: TokenRefreshService,
        private _router: Router
    ) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // Session endpoints set / read the HttpOnly refresh cookie, which a cross origin
        // call only stores and sends with credentials.
        if (this.isAuthEndpoint(request)) {
            request = request.clone({ withCredentials: true, setHeaders: { 'X-Token-Transport': 'cookie' } });
        }

        if (this.isAnonymous(request)) {
            return next.handle(request);
        }

        // Only calls to our own API get the header — a token has no business reaching
        // Google Maps or any other third party.
        if (!this.isApiRequest(request)) {
            return next.handle(request);
        }

        const needsRefreshFirst = this._tokenStorage.hasRefreshToken()
            && (this._tokenStorage.isAccessTokenExpired() || this._tokenStorage.isAccessTokenAboutToExpire());

        if (needsRefreshFirst) {
            return this._tokenRefresh.refresh().pipe(
                catchError((error) => this.refreshFailed(error)),
                switchMap((token) => next.handle(this.withToken(request, token)))
            );
        }

        const sentToken = this._tokenStorage.accessToken;
        return next.handle(this.withToken(request, sentToken)).pipe(
            catchError((error: HttpErrorResponse) => {
                if (error.status !== 401 || !this._tokenStorage.hasRefreshToken()) {
                    return throwError(() => error);
                }

                // Another request may have refreshed while this response was in flight.
                if (sentToken !== this._tokenStorage.accessToken) {
                    return next.handle(this.withToken(request, this._tokenStorage.accessToken));
                }
                return this._tokenRefresh.refresh().pipe(
                    catchError((refreshError) => this.refreshFailed(refreshError)),
                    switchMap((token) => next.handle(this.withToken(request, token)))
                );
            })
        );
    }

    private withToken(request: HttpRequest<any>, token: string | null): HttpRequest<any> {
        if (token == null || token === '') {
            return request;
        }

        return request.clone({
            setHeaders: {
                Authorization: 'Bearer ' + token
            }
        });
    }

    /** The session is gone: clear it and send the user to the right login page. */
    private giveUp(error: any): Observable<never> {
        this._tokenStorage.clear();

        const url = this._router.url || '';
        const loginUrl = url.startsWith('/partner') ? '/partner-auth/login' : '/auth/login';

        if (!url.startsWith('/auth') && !url.startsWith('/partner-auth')) {
            this._router.navigateByUrl(loginUrl);
        }

        return throwError(() => error);
    }

    private refreshFailed(error: any): Observable<never> {
        return !this._tokenStorage.hasRefreshToken() && error instanceof HttpErrorResponse
            && (error.status === 401 || error.status === 403)
            ? this.giveUp(error) : throwError(() => error);
    }

    private isAnonymous(request: HttpRequest<any>): boolean {
        const base = new URL(environment.apiUrl, window.location.origin);
        const url = new URL(request.url, window.location.origin);
        const prefix = base.pathname.replace(/\/$/, '');
        const path = url.pathname.slice(prefix.length).replace(/\/$/, '').toLowerCase();
        return url.origin === base.origin && TokenInterceptor.ANONYMOUS_PATHS.includes(path);
    }

    private isAuthEndpoint(request: HttpRequest<any>): boolean {
        const base = new URL(environment.apiUrl, window.location.origin);
        const url = new URL(request.url, window.location.origin);
        const prefix = base.pathname.replace(/\/$/, '').toLowerCase();
        const path = url.pathname.toLowerCase().slice(prefix.length);
        return url.origin === base.origin && (path.startsWith('/api/auth/') || path.startsWith('/api/partnerauth/'));
    }

    private isApiRequest(request: HttpRequest<any>): boolean {
        const base = new URL(environment.apiUrl, window.location.origin);
        const url = new URL(request.url, window.location.origin);
        const basePath = base.pathname.replace(/\/$/, '').toLowerCase();
        const path = url.pathname.toLowerCase();

        return url.origin === base.origin
            && (path.startsWith(basePath + '/api/')
                || path.startsWith(basePath + '/ocpp/'));
    }
}

