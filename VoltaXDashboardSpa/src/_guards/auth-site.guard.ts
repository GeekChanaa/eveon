import { CanActivateFn } from '@angular/router';
import { environment } from 'src/environments/environment';

/**
 * Sign in, sign up, password and verification pages live on the public site
 * (environment.authUrl), which serves the same /auth/... and /partner-auth/... paths.
 * Every in-app navigation to one of them, and every old email link, is forwarded there
 * with its query string intact.
 */
export const authSiteRedirect: CanActivateFn = (_route, state) => {
  window.location.replace(environment.authUrl + state.url);
  return false;
};

/**
 * The sign in page for a signed out visit to `url`: the partner one under /partner..., the customer one
 * otherwise. The public site sends the user back to `url` (?returnUrl=) once signed in, so a shared link to
 * /dashboard/users opens that page after the login, not the dashboard home.
 */
export function loginUrlFor(url: string): string {
  const page = url.startsWith('/partner') ? '/partner-auth/login' : '/auth/login';
  return isReturnable(url) ? `${page}?returnUrl=${encodeURIComponent(url)}` : page;
}

/** In-app pages worth coming back to: not "/", not the auth pages, never another site ("//evil.com"). */
function isReturnable(url: string): boolean {
  return /^\/(?![/\\])/.test(url) && url !== '/'
    && !url.startsWith('/auth') && !url.startsWith('/partner-auth');
}
