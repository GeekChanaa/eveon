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
