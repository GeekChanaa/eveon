import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { UrlTree } from '@angular/router';
import { TokenStorageService } from 'src/_services/token-storage.service';
import { AuthService } from 'src/_services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard  {
  constructor(private authService: AuthService, private router: Router, private tokens: TokenStorageService) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean | UrlTree> | boolean | UrlTree {
    const login = this.router.parseUrl(state.url.startsWith('/partner') ? '/partner-auth/login' : '/auth/login');
    if (!this.tokens.isAccessTokenExpired()) {
      return true;
    }
    if (!this.tokens.hasRefreshToken()) return login;
    return this.authService.refreshSession().pipe(
      map(() => true),
      catchError(() => of(this.tokens.hasRefreshToken() ? false : login))
    );
  }
}
