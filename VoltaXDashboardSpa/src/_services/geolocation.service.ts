import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface UserLocation {
  latitude: number;
  longitude: number;
  city: string;
  country: string;
}

interface LocationConsent {
  granted: boolean;
  at: string;
}

/**
 * The user's location, only ever requested after an explicit action and only kept in
 * localStorage when the user agreed to it. Without consent a location lives in memory for
 * the current page only. {@link revokeConsent} forgets both the consent and the location.
 */
@Injectable({
  providedIn: 'root'
})
export class GeolocationService {

  static readonly CONSENT_KEY = 'locationConsent';
  static readonly LOCATION_KEY = 'userLocation';
  // Written by the previous implementation without asking.
  private static readonly LEGACY_KEYS = ['userLatitude', 'userLongitude', 'userLocationCity', 'userLocationCountry', 'lastRetrieved'];
  private static readonly MAX_AGE_MS = 6 * 60 * 60 * 1000;

  private memoryLocation: UserLocation | null = null;
  readonly consent$ = new BehaviorSubject<LocationConsent | null>(this.readConsent());

  constructor() {
    GeolocationService.LEGACY_KEYS.forEach(key => this.remove(key));
  }

  get hasConsent(): boolean {
    return this.consent$.value?.granted === true;
  }

  /** Records the user's explicit choice to let the app remember their location. */
  grantConsent(): void {
    const consent = { granted: true, at: new Date().toISOString() };
    this.write(GeolocationService.CONSENT_KEY, JSON.stringify(consent));
    this.consent$.next(consent);
  }

  /** Forgets the consent and every stored location. */
  revokeConsent(): void {
    this.remove(GeolocationService.CONSENT_KEY);
    this.remove(GeolocationService.LOCATION_KEY);
    this.memoryLocation = null;
    this.consent$.next(null);
  }

  /** Last known location without prompting: memory first, then storage when consented. */
  cachedLocation(): UserLocation | null {
    if (this.memoryLocation) return this.memoryLocation;
    if (!this.hasConsent) return null;

    try {
      const stored = JSON.parse(localStorage.getItem(GeolocationService.LOCATION_KEY) || 'null');
      if (stored && Date.now() - Date.parse(stored.retrievedAt) < GeolocationService.MAX_AGE_MS) {
        return stored.location as UserLocation;
      }
    } catch { }
    return null;
  }

  /**
   * Asks the browser for the position. Call it from a user action (button click) only;
   * the browser shows its own permission prompt on top of the app's consent.
   */
  requestLocation(): Promise<UserLocation> {
    const cached = this.cachedLocation();
    if (cached) return Promise.resolve(cached);

    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        reject('Geolocation is not supported by this browser.');
        return;
      }

      navigator.geolocation.getCurrentPosition(position => {
        const { latitude, longitude } = position.coords;
        this.getLocationDetails(latitude, longitude).then(details => {
          const location: UserLocation = { latitude, longitude, city: details.city, country: details.country };
          this.memoryLocation = location;
          if (this.hasConsent) {
            this.write(GeolocationService.LOCATION_KEY, JSON.stringify({ location, retrievedAt: new Date().toISOString() }));
          }
          resolve(location);
        });
      }, err => reject(err));
    });
  }

  private getLocationDetails(latitude: number, longitude: number): Promise<{ city: string, country: string }> {
    const url = `https://maps.googleapis.com/maps/api/geocode/json?latlng=${latitude},${longitude}&key=${environment.googleGeocodingApiKey}`;

    return fetch(url)
      .then(response => response.json())
      .then(data => {
        if (data.status !== 'OK') throw new Error('Unable to retrieve location details');
        const components = data.results[0].address_components;
        const country = components.find((c: any) => c.types.includes('country'));
        const city = components.find((c: any) => c.types.includes('locality'));
        return { country: country ? country.long_name : '', city: city ? city.long_name : '' };
      })
      .catch(error => {
        console.error('Error in fetching location details:', error);
        return { country: '', city: '' };
      });
  }

  private readConsent(): LocationConsent | null {
    try {
      const consent = JSON.parse(localStorage.getItem(GeolocationService.CONSENT_KEY) || 'null');
      return consent?.granted === true ? consent : null;
    } catch {
      return null;
    }
  }

  private write(key: string, value: string) {
    try { localStorage.setItem(key, value); } catch { }
  }

  private remove(key: string) {
    try { localStorage.removeItem(key); } catch { }
  }
}
