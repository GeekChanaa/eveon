import { of } from 'rxjs';
import { MyProfile } from 'src/_models/portal';
import { CustomerPortalService } from './customer-portal.service';
import { PROFILE_PROMPT_SEEN_KEY, ProfileCompletionService, profileWarnings } from './profile-completion.service';

const complete: MyProfile = {
  firstName: 'Sara', lastName: 'B', email: 'sara@test.ma', phone: '+212612345678', birthday: null, imageUrl: null,
  isEmailVerified: true, isPhoneNumberVerified: true, memberSince: '2026-01-01', gender: null, city: null,
  electricVehicleModelId: null, electricVehicleModelName: null
};

describe('ProfileCompletionService', () => {
  let portal: jasmine.SpyObj<CustomerPortalService>;
  let service: ProfileCompletionService;

  beforeEach(() => {
    sessionStorage.removeItem(PROFILE_PROMPT_SEEN_KEY);
    localStorage.removeItem('vx.profilePrompt.dismissed.7');
    portal = jasmine.createSpyObj<CustomerPortalService>('CustomerPortalService', ['profile']);
    service = new ProfileCompletionService(portal);
  });

  afterEach(() => {
    sessionStorage.removeItem(PROFILE_PROMPT_SEEN_KEY);
    localStorage.removeItem('vx.profilePrompt.dismissed.7');
  });

  it('lists an unverified email, a missing phone and an unverified phone', () => {
    expect(profileWarnings(complete)).toEqual([]);
    expect(profileWarnings({ ...complete, isEmailVerified: false, phone: null, isPhoneNumberVerified: false }))
      .toEqual(['emailNotVerified', 'phoneMissing']);
    expect(profileWarnings({ ...complete, isPhoneNumberVerified: false })).toEqual(['phoneNotVerified']);
    expect(profileWarnings(null)).toEqual([]);
  });

  it('prompts once per session while something is missing', () => {
    portal.profile.and.returnValue(of({ ...complete, isEmailVerified: false }));
    service.load().subscribe();

    expect(service.shouldPrompt(7)).toBeTrue();
    service.markPrompted();
    expect(service.shouldPrompt(7)).toBeFalse();
  });

  it('stops prompting after "don\'t show again" but keeps the warnings', () => {
    portal.profile.and.returnValue(of({ ...complete, phone: null }));
    service.load().subscribe();
    service.dismissForever(7);

    expect(service.shouldPrompt(7)).toBeFalse();
    let warnings: string[] = [];
    service.warnings$.subscribe(w => warnings = w);
    expect(warnings).toEqual(['phoneMissing']);
  });

  it('never prompts a complete account', () => {
    portal.profile.and.returnValue(of(complete));
    service.load().subscribe();
    expect(service.shouldPrompt(7)).toBeFalse();
  });

  it('forwards open requests to the dialog', () => {
    const steps: string[] = [];
    service.openRequests$.subscribe(step => steps.push(step));
    service.open();
    service.open('details');
    expect(steps).toEqual(['contact', 'details']);
  });
});
