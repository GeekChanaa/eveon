import { inject } from '@angular/core';
import { CanActivateFn, CanActivateChildFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AccessService } from 'src/_services/access.service';
export const dashboardPermissionGuard: CanActivateFn & CanActivateChildFn = (_route, state) => {
  const access = inject(AccessService);
  const router = inject(Router);
  return access.load().pipe(map(() => access.canUrl(state.url) || router.createUrlTree(['/access-denied'])),
    catchError(() => of(router.createUrlTree(['/access-denied']))));
};

/** The partner portal API only serves partner accounts, so administrators are not let in either. */
export const partnerPermissionGuard: CanActivateFn & CanActivateChildFn = () => {
  const access = inject(AccessService);
  const router = inject(Router);
  return access.load().pipe(map(info => (info.role === 'Partner' && !!info.partnerId) || router.createUrlTree(['/access-denied'])),
    catchError(() => of(router.createUrlTree(['/access-denied']))));
};
