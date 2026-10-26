import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { Subject, Subscription, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap } from 'rxjs/operators';
import { AddressGeocodingService, AddressSuggestion } from 'src/_services/address-geocoding.service';
import { CityService } from 'src/_services/city.service';

// Google's English city names that differ from the spelling used by the city list.
const CITY_ALIASES: { [google: string]: string[] } = {
  marrakesh: ['marrakech'],
  tangier: ['tanger'],
  fez: ['fes'],
  tetouan: ['tetuan'],
  'el jadida': ['eljadida'],
  laayoune: ['layoune', 'el aaiun']
};

@Component({
  selector: 'app-create-charging-station-address',
  templateUrl: './create-charging-station-address.component.html',
  styleUrls: ['./create-charging-station-address.component.sass']
})
export class CreateChargingStationAddressComponent implements OnInit, OnDestroy {

  @Input() form : FormGroup = new FormGroup({});
  @Output() nextStep : EventEmitter<void> =  new EventEmitter();
  @Output() previousStepEvent : EventEmitter<void> =  new EventEmitter();
  cities : any [] = [];

  query = '';
  suggestions: AddressSuggestion[] = [];
  activeSuggestion = -1;
  showSuggestions = false;
  searching = false;
  searched = false;
  searchUnavailable = false;

  resolvingPin = false;
  pinMessage = '';
  submitted = false;

  private query$ = new Subject<string>();
  private pin$ = new Subject<{ latitude: number; longitude: number }>();
  private subscriptions = new Subscription();

  constructor(
    private _cityService : CityService,
    private _geocoding : AddressGeocodingService
  ) { }

  ngOnInit() {
    this.getCities();

    this.subscriptions.add(this.query$.pipe(
      map(query => query.trim()),
      debounceTime(350),
      tap(query => {
        this.searching = query.length >= 3;
        this.searched = false;
      }),
      switchMap(query => this._geocoding.search(query).pipe(
        tap(() => this.searchUnavailable = false),
        catchError(error => {
          console.error('Address search failed:', error);
          this.searchUnavailable = true;
          return of([] as AddressSuggestion[]);
        }),
        map(results => ({ query, results }))
      ))
    ).subscribe(({ query, results }) => {
      this.searching = false;
      this.searched = query.length >= 3;
      this.suggestions = results;
      this.activeSuggestion = results.length ? 0 : -1;
      this.showSuggestions = this.searched;
    }));

    // Latest pin wins: a slow lookup for an older click never overwrites a newer one.
    this.subscriptions.add(this.pin$.pipe(
      tap(() => this.resolvingPin = true),
      switchMap(pin => this._geocoding.reverse(pin.latitude, pin.longitude).pipe(
        catchError(error => {
          console.error('Reverse geocoding failed:', error);
          return of(null);
        })
      ))
    ).subscribe(address => {
      this.resolvingPin = false;
      if (address) {
        this.applyAddress(address, false);
        this.pinMessage = 'Address filled from the pin. Adjust it if needed.';
      } else {
        this.pinMessage = 'No address found at this spot. Fill in the fields manually.';
      }
    }));
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  getCities(){
    this._cityService.getAllMoroccoCityNames().subscribe((data) => {
      this.cities = data.map((item : any) => {return {name: item.name, id: item.name}})
    })
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

  get latitude(): number | null {
    return this.form.get('latitude')?.value ?? null;
  }

  get longitude(): number | null {
    return this.form.get('longitude')?.value ?? null;
  }

  get hasLocation(): boolean {
    return this.latitude != null && this.longitude != null;
  }

  onQueryChange(value: string) {
    this.query$.next(value || '');
    if (!value || value.trim().length < 3) {
      this.suggestions = [];
      this.showSuggestions = false;
    }
  }

  onSearchKeydown(event: KeyboardEvent) {
    if (!this.showSuggestions || !this.suggestions.length) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeSuggestion = (this.activeSuggestion + 1) % this.suggestions.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeSuggestion = (this.activeSuggestion - 1 + this.suggestions.length) % this.suggestions.length;
        break;
      case 'Enter':
        event.preventDefault();
        this.pickSuggestion(this.suggestions[Math.max(this.activeSuggestion, 0)]);
        break;
      case 'Escape':
        this.showSuggestions = false;
        break;
    }
  }

  openSuggestions() {
    this.showSuggestions = this.searched && this.query.trim().length >= 3;
  }

  closeSuggestions() {
    this.showSuggestions = false;
  }

  pickSuggestion(suggestion: AddressSuggestion) {
    this.query = suggestion.label;
    this.showSuggestions = false;
    this.pinMessage = 'Pin placed at the address. Drag it onto the charging entrance if needed.';
    this.applyAddress(suggestion, true);
  }

  onLocationSelected(event: { latitude: number; longitude: number }) {
    this.form.patchValue({
      latitude: event.latitude,
      longitude: event.longitude
    });
    this.form.get('latitude')?.markAsTouched();
    this.pin$.next(event);
  }

  saveInformations(){
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.nextStep.emit();
  }

  previousStep = () => this.previousStepEvent.emit();

  // Fills the fields from a geocoded address. Blank parts never erase what the user typed.
  private applyAddress(address: AddressSuggestion, moveLocation: boolean) {
    const patch: any = {};
    if (address.street && !CreateChargingStationAddressComponent.isPlaceholderStreet(address.street)) {
      patch.address = address.street;
    }
    if (address.city) {
      patch.city = this.matchCity(address.city);
    }
    if (address.zipCode) {
      patch.zipCode = address.zipCode;
    }
    if (moveLocation) {
      patch.latitude = address.latitude;
      patch.longitude = address.longitude;
    }
    this.form.patchValue(patch);
    Object.keys(patch).forEach(key => this.form.get(key)?.markAsTouched());
  }

  // Picks the spelling from the city list so the dropdown shows it as selected.
  private matchCity(city: string): string {
    const normalized = CreateChargingStationAddressComponent.normalize(city);
    const candidates = [normalized, ...(CITY_ALIASES[normalized] || [])];
    const match = this.cities.find(option =>
      candidates.includes(CreateChargingStationAddressComponent.normalize(option.name)));
    return match ? match.id : city;
  }

  private static normalize(value: string): string {
    return (value || '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[-']/g, ' ').trim().toLowerCase();
  }

  // Google returns "Unnamed Road" or a plus code when the spot has no street.
  private static isPlaceholderStreet(street: string): boolean {
    return /unnamed road/i.test(street) || /^[A-Z0-9]{4,}\+[A-Z0-9]{2,}/.test(street);
  }
}
