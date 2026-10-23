import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { AccessInfo, AccessService } from 'src/_services/access.service';
import { partnerPermissionGuard } from './dashboard-permission.guard';
import { IndexComponent } from 'src/app/global/index/index.component';

describe('Portal access', () => {
  const info = (role: string, extra: Partial<AccessInfo> = {}): AccessInfo =>
    ({ userId: 1, roleId: 1, role, isAdmin: role === 'Admin', permissions: [], firstName: '', lastName: '', email: '', ...extra });

  async function partnerGuard(load: Observable<AccessInfo>): Promise<boolean | UrlTree> {
    TestBed.configureTestingModule({ imports: [VxTestingModule], providers: [{ provide: AccessService, useValue: { load: () => load } }] });
    const result = TestBed.runInInjectionContext(() => partnerPermissionGuard({} as any, { url: '/partner-dashboard' } as any));
    return firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('lets partner accounts with a partner into the partner portal', async () => {
    expect(await partnerGuard(of(info('Partner', { partnerId: 4 })))).toBeTrue();
  });

  it('sends administrators, customers and partner accounts without a partner to the access denied page', async () => {
    for (const account of [info('Admin'), info('Customer'), info('Partner')]) {
      TestBed.resetTestingModule();
      const result = await partnerGuard(of(account));
      expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/access-denied');
    }
  });

  it('denies access when the access information cannot be loaded', async () => {
    const result = await partnerGuard(throwError(() => new Error('401')));
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/access-denied');
  });

  it('opens each account on its own space from "/"', () => {
    expect(IndexComponent.home('Partner', true, false)).toBe('/partner-dashboard');
    expect(IndexComponent.home('Partner', false, false)).toBe('/my-dashboard');
    expect(IndexComponent.home('Admin', false, true)).toBe('/dashboard');
    expect(IndexComponent.home('Support', false, true)).toBe('/dashboard');
    expect(IndexComponent.home('Customer', false, false)).toBe('/my-dashboard');
  });
});
