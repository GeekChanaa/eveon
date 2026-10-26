import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject, map, tap } from 'rxjs';
import { MyProfile } from 'src/_models/portal';
import { CustomerPortalService } from './customer-portal.service';

/** What still stands between the customer and a complete account. */
export type ProfileWarning = 'emailNotVerified' | 'phoneMissing' | 'phoneNotVerified';

/** Where the completion dialog opens: verifying email / phone, or the optional details. */
export type CompletionStep = 'contact' | 'details';

/** Set once the prompt was shown in this browser tab; cleared on sign out so the next sign in shows it again. */
export const PROFILE_PROMPT_SEEN_KEY = 'vx.profilePrompt.seen';
const DISMISSED_KEY = 'vx.profilePrompt.dismissed.';

export function profileWarnings(profile: MyProfile | null): ProfileWarning[] {
  if (!profile) return [];
  const warnings: ProfileWarning[] = [];
  if (!profile.isEmailVerified) warnings.push('emailNotVerified');
  if (!profile.phone) warnings.push('phoneMissing');
  else if (!profile.isPhoneNumberVerified) warnings.push('phoneNotVerified');
  return warnings;
}

/**
 * The signed-in customer's profile as far as completing it goes: the warnings (unverified email, missing or
 * unverified phone) shown on the home page and in the notifications, and the completion dialog shown at sign in
 * until the customer asks not to see it again. Loaded by the customer dashboard shell; empty everywhere else.
 */
@Injectable({ providedIn: 'root' })
export class ProfileCompletionService {
  private readonly profileSubject = new BehaviorSubject<MyProfile | null>(null);
  private readonly openRequests = new Subject<CompletionStep>();

  readonly profile$: Observable<MyProfile | null> = this.profileSubject.asObservable();
  readonly warnings$: Observable<ProfileWarning[]> = this.profile$.pipe(map(profileWarnings));
  /** Asks the dialog (in the customer dashboard shell) to open at a step. */
  readonly openRequests$: Observable<CompletionStep> = this.openRequests.asObservable();

  constructor(private portal: CustomerPortalService) {}

  get profile(): MyProfile | null { return this.profileSubject.value; }

  /** Fetches the profile (at start, after a verification or a save) and publishes it on profile$. */
  load(): Observable<MyProfile> {
    return this.portal.profile().pipe(tap(profile => this.profileSubject.next(profile)));
  }

  clear(): void { this.profileSubject.next(null); }

  open(step: CompletionStep = 'contact'): void { this.openRequests.next(step); }

  /** True when the prompt should pop up now: something is missing, not shown yet this session, not turned off. */
  shouldPrompt(userId: number | null): boolean {
    return profileWarnings(this.profile).length > 0 && !this.read(sessionStorage, PROFILE_PROMPT_SEEN_KEY)
      && !(userId != null && this.read(localStorage, DISMISSED_KEY + userId));
  }

  markPrompted(): void { this.write(sessionStorage, PROFILE_PROMPT_SEEN_KEY, '1'); }

  /** "Don't show again": the warnings stay on the home page and in the notifications, only the pop-up stops. */
  dismissForever(userId: number | null): void {
    if (userId != null) this.write(localStorage, DISMISSED_KEY + userId, '1');
  }

  private read(storage: Storage, key: string): boolean {
    try { return storage.getItem(key) === '1'; } catch { return false; }
  }

  private write(storage: Storage, key: string, value: string): void {
    try { storage.setItem(key, value); } catch { /* private mode: the prompt simply shows again */ }
  }
}
