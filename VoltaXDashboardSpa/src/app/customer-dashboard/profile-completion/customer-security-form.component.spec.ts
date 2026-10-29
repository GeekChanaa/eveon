import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { LinkedAccountsDto } from 'src/_models/_dtos/linked-accounts-dto';
import { AuthService } from 'src/_services/auth.service';
import { CustomerSecurityFormComponent, SECURITY_RETURN_URL, isStrongPassword } from './customer-security-form.component';

const accounts = (extra: Partial<LinkedAccountsDto> = {}): LinkedAccountsDto =>
  ({ googleLinked: false, googleEmail: null, hasPassword: true, authProvider: 'Local', ...extra });

describe('CustomerSecurityFormComponent', () => {
  let auth: jasmine.SpyObj<AuthService>;
  const route = { snapshot: { queryParamMap: convertToParamMap({}) } };

  function create(linked: LinkedAccountsDto = accounts(), query: Record<string, string> = {}) {
    auth.getLinkedAccounts.and.returnValue(of(linked));
    route.snapshot.queryParamMap = convertToParamMap(query);
    const fixture = TestBed.createComponent(CustomerSecurityFormComponent);
    fixture.componentInstance.email = 'me@test.ma';
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService',
      ['getLinkedAccounts', 'changePassword', 'resetPasswordRequest', 'startGoogleLink', 'unlinkGoogle', 'getAuthInformation']);
    auth.getAuthInformation.and.returnValue({ nameid: '7' });
    return configureVxTest([], { imports: [CustomerSecurityFormComponent], providers: [{ provide: AuthService, useValue: auth }, { provide: ActivatedRoute, useValue: route }] });
  });

  it('applies the same strength rule as the API', () => {
    expect(isStrongPassword('Brand-new-pass-2')).toBeTrue();
    expect(isStrongPassword('short-A1')).toBeTrue();
    expect(isStrongPassword('Sh-1')).toBeFalse();
    expect(isStrongPassword('no-upper-case-1')).toBeFalse();
    expect(isStrongPassword('NoSpecialChar1')).toBeFalse();
  });

  it('does not call the API while the form is invalid', () => {
    const form = create().componentInstance;
    form.startPasswordChange();

    form.current = 'Current-pass-1';
    form.next = 'Brand-new-pass-2';
    form.confirm = 'Brand-new-pass-X';
    form.changePassword();
    form.next = form.confirm = 'Current-pass-1';
    form.changePassword();
    form.next = form.confirm = 'weak';
    form.changePassword();

    expect(auth.changePassword).not.toHaveBeenCalled();
  });

  it('sends the caller id with the current and new passwords', () => {
    auth.changePassword.and.returnValue(of({}));
    const form = create().componentInstance;
    form.startPasswordChange();
    form.current = 'Current-pass-1';
    form.next = form.confirm = 'Brand-new-pass-2';

    form.changePassword();

    expect(auth.changePassword).toHaveBeenCalledOnceWith(
      { id: 7, currentPassword: 'Current-pass-1', newPassword: 'Brand-new-pass-2', newPasswordCheck: 'Brand-new-pass-2' });
  });

  it('explains a wrong current password and too many attempts, and clears the current password', () => {
    const form = create().componentInstance;
    form.startPasswordChange();
    form.next = form.confirm = 'Brand-new-pass-2';

    auth.changePassword.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    form.current = 'Wrong-pass-1';
    form.changePassword();
    expect(form.passwordError).toBe('portal.security.wrongCurrent');
    expect(form.current).toBe('');

    auth.changePassword.and.returnValue(throwError(() => new HttpErrorResponse({ status: 429 })));
    form.current = 'Wrong-pass-2';
    form.changePassword();
    expect(form.passwordError).toBe('portal.completion.tooMany');
  });

  it('offers to set a password by email when the account was created with Google', () => {
    auth.resetPasswordRequest.and.returnValue(of({}));
    const fixture = create(accounts({ hasPassword: false, googleLinked: true, googleEmail: 'me@gmail.com', authProvider: 'Google' }));
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('input[type=password]')).toBeNull();
    fixture.componentInstance.sendSetPasswordEmail();

    expect(auth.resetPasswordRequest).toHaveBeenCalledOnceWith('me@test.ma');
    expect(fixture.componentInstance.resetSent).toBeTrue();
  });

  it('links Google and comes back to the profile', () => {
    const fixture = create();

    fixture.componentInstance.linkGoogle();

    expect(auth.startGoogleLink).toHaveBeenCalledOnceWith(SECURITY_RETURN_URL);
  });

  it('confirms a link that just happened', () => {
    const fixture = create(accounts({ googleLinked: true, googleEmail: 'me@gmail.com' }), { google: 'linked' });

    expect(fixture.componentInstance.justLinked).toBeTrue();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('me@gmail.com');
  });

  it('unlinks Google only when a password is set', () => {
    auth.unlinkGoogle.and.returnValue(of(accounts()));
    const withPassword = create(accounts({ googleLinked: true, googleEmail: 'me@gmail.com' }));
    withPassword.componentInstance.unlinkGoogle();
    expect(withPassword.componentInstance.accounts?.googleLinked).toBeFalse();

    const googleOnly = create(accounts({ googleLinked: true, hasPassword: false }));
    const button = Array.from((googleOnly.nativeElement as HTMLElement).querySelectorAll('button'))
      .find(b => b.textContent?.includes('portal.security.unlink'));
    expect(button?.disabled).toBeTrue();
  });
});
