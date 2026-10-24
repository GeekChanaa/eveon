import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { AccessService } from 'src/_services/access.service';
import { AuthService } from 'src/_services/auth.service';
import { TokenRefreshService } from 'src/_services/token-refresh.service';
import { TwoFactorSettingsComponent } from '../dashboard/profile/profile-security/two-factor-settings/two-factor-settings.component';
import { GoogleCallbackComponent } from './google-callback/google-callback.component';
import { SessionComponent } from './session/session.component';

describe('Sign-in pages of the dashboard', () => {
  describe('SessionComponent', () => {
    let refresh: jasmine.Spy;
    let access: jasmine.SpyObj<AccessService>;

    beforeEach(async () => {
      refresh = jasmine.createSpy('refresh');
      access = jasmine.createSpyObj<AccessService>('AccessService', ['load', 'canUrl']);
      await configureVxTest([SessionComponent], {
        providers: [
          { provide: TokenRefreshService, useValue: { refresh } },
          { provide: AccessService, useValue: access },
          { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ portal: 'partner' }) } } }
        ]
      });
    });

    it('says it is signing in, then opens the partner portal for a partner', () => {
      refresh.and.returnValue(of({}));
      access.load.and.returnValue(of({ userId: 2, roleId: 5, role: 'Partner', isAdmin: false, permissions: [], firstName: 'A', lastName: 'B', email: 'a@b.ma', partnerId: 3 }));
      const navigate = spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
      const fixture = TestBed.createComponent(SessionComponent);

      expect(fixture.componentInstance.message).toBe('auth.session.signingIn');
      fixture.detectChanges();

      expect(navigate).toHaveBeenCalledWith('/partner-dashboard', { replaceUrl: true });
    });

    it('explains and goes back to the partner sign-in when the session cannot be opened', () => {
      refresh.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
      const fixture = TestBed.createComponent(SessionComponent);
      const redirect = spyOn(fixture.componentInstance, 'redirect');
      fixture.detectChanges();

      expect(fixture.componentInstance.message).toBe('auth.session.failed');
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('auth.session.failed');
      expect(redirect.calls.mostRecent().args[0]).toMatch(/\/partner-auth\/login$/);
    });
  });

  describe('GoogleCallbackComponent', () => {
    beforeEach(() => configureVxTest([GoogleCallbackComponent], {
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { fragment: '', queryParams: {} } } },
        { provide: AuthService, useValue: jasmine.createSpyObj('AuthService', ['completeExternalLogin']) },
        { provide: AccessService, useValue: jasmine.createSpyObj('AccessService', ['load', 'canUrl', 'can']) }
      ]
    }));

    it('shows a translated failure when Google returned no token', () => {
      spyOn(window.history, 'replaceState');
      const fixture = TestBed.createComponent(GoogleCallbackComponent);
      fixture.detectChanges();

      expect(fixture.componentInstance.errorMessage).toBe('auth.google.noToken');
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('auth.google.failed');
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('auth.google.backToLogin');
    });
  });

  describe('TwoFactorSettingsComponent', () => {
    let auth: jasmine.SpyObj<AuthService>;

    beforeEach(async () => {
      auth = jasmine.createSpyObj<AuthService>('AuthService', ['getTwoFactorStatus', 'enrollTwoFactor', 'confirmTwoFactor', 'disableTwoFactor']);
      await configureVxTest([TwoFactorSettingsComponent], { providers: [{ provide: AuthService, useValue: auth }] });
    });

    it('is written with translation keys only', () => {
      auth.getTwoFactorStatus.and.returnValue(of({ eligible: true, enabled: false, required: true, recoveryCodesLeft: 0 } as any));
      const fixture = TestBed.createComponent(TwoFactorSettingsComponent);
      fixture.detectChanges();
      const text = (fixture.nativeElement as HTMLElement).textContent!;

      expect(text).toContain('auth.twoFactor.title');
      expect(text).toContain('auth.twoFactor.required');
      expect(text).toContain('auth.twoFactor.setUp');
      expect(text).not.toMatch(/Two factor|Set up|Protect your account/);
    });

    it('translates its own errors and keeps the API\'s explanation', () => {
      auth.getTwoFactorStatus.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
      const fixture = TestBed.createComponent(TwoFactorSettingsComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.error).toBe('auth.twoFactor.loadFailed');

      auth.confirmTwoFactor.and.returnValue(throwError(() => new HttpErrorResponse({ status: 429 })));
      fixture.componentInstance.code = '123456';
      fixture.componentInstance.confirmEnrollment();
      expect(fixture.componentInstance.error).toBe('auth.twoFactor.tooManyAttempts');

      auth.confirmTwoFactor.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'Invalid code.' } })));
      fixture.componentInstance.confirmEnrollment();
      expect(fixture.componentInstance.error).toBe('Invalid code.');
    });
  });
});
