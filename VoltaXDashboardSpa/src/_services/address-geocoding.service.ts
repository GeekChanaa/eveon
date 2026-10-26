import { Injectable } from '@angular/core';
import { Observable, from, of } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AddressSuggestion {
  // Full formatted address, shown in the suggestion list.
  label: string;
  street: string;
  city: string;
  zipCode: string;
  latitude: number;
  longitude: number;
}

/**
 * Address lookup through the Google Geocoding REST API, restricted to Morocco.
 * Uses fetch rather than HttpClient so the API interceptors (auth header, error popups)
 * never touch a third-party request.
 */
@Injectable({
  providedIn: 'root'
})
export class AddressGeocodingService {

  private static readonly BASE_URL = 'https://maps.googleapis.com/maps/api/geocode/json';
  private static readonly COUNTRY = 'MA';

  /** Addresses matching free text, best match first. */
  search(query: string): Observable<AddressSuggestion[]> {
    const text = query.trim();
    if (text.length < 3) return of([]);

    const params = new URLSearchParams({
      address: text,
      components: `country:${AddressGeocodingService.COUNTRY}`,
      region: AddressGeocodingService.COUNTRY.toLowerCase(),
      language: 'en'
    });
    return from(this.request(params).then(results => results.slice(0, 6)));
  }

  /** The address at a point, or null when Google has none. */
  reverse(latitude: number, longitude: number): Observable<AddressSuggestion | null> {
    const params = new URLSearchParams({ latlng: `${latitude},${longitude}`, language: 'en' });
    return from(this.request(params).then(results => {
      const first = results[0];
      // Keep the exact pin rather than the geocoded centroid of the address.
      return first ? { ...first, latitude, longitude } : null;
    }));
  }

  private request(params: URLSearchParams): Promise<AddressSuggestion[]> {
    params.set('key', environment.googleGeocodingApiKey);
    return fetch(`${AddressGeocodingService.BASE_URL}?${params}`)
      .then(response => response.json())
      .then(data => {
        if (data.status === 'ZERO_RESULTS') return [];
        if (data.status !== 'OK') throw new Error(data.error_message || data.status);
        return (data.results as any[]).map(result => this.toSuggestion(result));
      });
  }

  private toSuggestion(result: any): AddressSuggestion {
    const components: any[] = result.address_components || [];
    const find = (...types: string[]) =>
      types.map(type => components.find(c => c.types.includes(type))?.long_name).find(Boolean) || '';

    const route = find('route');
    const number = find('street_number');
    const street = route
      ? [number, route].filter(Boolean).join(' ')
      : find('premise', 'point_of_interest', 'establishment', 'neighborhood', 'sublocality_level_1', 'sublocality');

    return {
      label: result.formatted_address,
      street: street || result.formatted_address.split(',')[0],
      city: find('locality', 'administrative_area_level_2', 'administrative_area_level_1'),
      zipCode: find('postal_code'),
      latitude: result.geometry.location.lat,
      longitude: result.geometry.location.lng
    };
  }
}
