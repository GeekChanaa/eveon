import { Component, ElementRef, ViewChild, AfterViewInit, Input, EventEmitter, Output, OnDestroy, OnChanges, SimpleChanges, NgZone } from '@angular/core';
import { DARK_MAP_STYLES, LIGHT_MAP_STYLES, stationPinUrl } from './map-styles';

// Declare google as any to avoid TypeScript errors
declare const google: any;

// Morocco as a whole, shown until a location is picked.
const DEFAULT_CENTER = { lat: 31.7917, lng: -7.0926 };
const DEFAULT_ZOOM = 6;
const PIN_ZOOM = 18;
const MAPS_LOAD_TIMEOUT_MS = 15000;

@Component({
  selector: 'app-map-picker',
  templateUrl: './map-picker.component.html',
  styleUrls: ['./map-picker.component.sass']
})
export class MapPickerComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer', { static: false }) gmap!: ElementRef;

  @Input() lat: number | null = null;
  @Input() lng: number | null = null;
  @Input() noEdit: boolean = false;
  // Asks for the browser position on load. Prefer the "Use my location" button, which only
  // asks after the user clicks it.
  @Input() useCurrentLocation: boolean = false;
  // Highlights the map when the form was submitted without a location.
  @Input() showError: boolean = false;
  @Input() height: string = '420px';
  @Output() locationSelected: EventEmitter<{ latitude: number; longitude: number; timestamp: string }> = new EventEmitter();

  map: any;
  marker: any;
  mapReady = false;
  loadFailed = false;
  locating = false;
  locateError = '';

  private isDestroyed = false;
  private themeObserver?: MutationObserver;

  constructor(private _zone: NgZone) { }

  get hasLocation(): boolean {
    return MapPickerComponent.isValid(this.lat, this.lng);
  }

  ngAfterViewInit() {
    this.waitForMaps()
      .then(() => this.initializeMap())
      .catch(error => {
        console.error('Error initializing map:', error);
        this.loadFailed = true;
      });
  }

  ngOnChanges(changes: SimpleChanges) {
    // API models sometimes carry coordinates as strings; Maps only accepts numbers.
    this.lat = this.lat != null && (this.lat as any) !== '' ? Number(this.lat) : null;
    this.lng = this.lng != null && (this.lng as any) !== '' ? Number(this.lng) : null;

    // Skip the echo of our own click or drag coming back through the parent form.
    if ((changes['lat'] || changes['lng']) && this.mapReady && this.hasLocation && !this.markerIsAt(this.lat!, this.lng!)) {
      this.placeMarker(this.lat!, this.lng!, true);
    }
  }

  ngOnDestroy() {
    this.isDestroyed = true;
    this.themeObserver?.disconnect();
    try {
      if (this.marker) google.maps.event.clearInstanceListeners(this.marker);
      if (this.map) google.maps.event.clearInstanceListeners(this.map);
    } catch (error) {
      console.error('Error during cleanup:', error);
    }
  }

  /** Centers the map on the browser position and drops the pin there. */
  locateMe() {
    if (!navigator.geolocation) {
      this.locateError = 'Your browser does not support location.';
      return;
    }
    this.locating = true;
    this.locateError = '';
    navigator.geolocation.getCurrentPosition(
      position => this._zone.run(() => {
        this.locating = false;
        this.selectLocation(position.coords.latitude, position.coords.longitude, true);
      }),
      error => this._zone.run(() => {
        this.locating = false;
        this.locateError = error.code === error.PERMISSION_DENIED
          ? 'Location access was denied. Search the address or click the map instead.'
          : 'Your location could not be found. Search the address or click the map instead.';
      }),
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
  }

  // Public method to update map center
  public updateMapCenter(lat: number, lng: number) {
    if (this.mapReady && MapPickerComponent.isValid(lat, lng)) {
      this.lat = lat;
      this.lng = lng;
      this.placeMarker(lat, lng, true);
    }
  }

  // Public method to get current location
  public getCurrentLocation(): { lat: number; lng: number } | null {
    return this.hasLocation ? { lat: this.lat!, lng: this.lng! } : null;
  }

  // The Maps script is appended asynchronously in main.ts, so it may not be there yet.
  private waitForMaps(): Promise<void> {
    const startedAt = Date.now();
    return new Promise((resolve, reject) => {
      const check = () => {
        if (this.isDestroyed) return;
        if ((window as any).google?.maps?.Map) return resolve();
        if (Date.now() - startedAt > MAPS_LOAD_TIMEOUT_MS) return reject(new Error('Google Maps API not loaded'));
        setTimeout(check, 100);
      };
      check();
    });
  }

  private initializeMap() {
    if (this.isDestroyed || !this.gmap?.nativeElement) return;

    const dark = MapPickerComponent.isDarkTheme();
    const center = this.hasLocation ? { lat: this.lat!, lng: this.lng! } : DEFAULT_CENTER;

    this.map = new google.maps.Map(this.gmap.nativeElement, {
      center,
      zoom: this.hasLocation ? PIN_ZOOM : DEFAULT_ZOOM,
      styles: dark ? DARK_MAP_STYLES : LIGHT_MAP_STYLES,
      backgroundColor: dark ? '#0E1E3F' : '#F3F5F9',
      // Clicking a shop or landmark icon must drop the pin, not open its info window.
      clickableIcons: this.noEdit,
      streetViewControl: false,
      fullscreenControl: true,
      mapTypeControl: true,
      mapTypeControlOptions: {
        mapTypeIds: ['roadmap', 'hybrid'],
        position: google.maps.ControlPosition.TOP_RIGHT,
        style: google.maps.MapTypeControlStyle.HORIZONTAL_BAR
      },
      fullscreenControlOptions: { position: google.maps.ControlPosition.RIGHT_BOTTOM },
      gestureHandling: 'cooperative'
    });

    if (this.hasLocation) {
      this.createMarker(center);
    }

    if (!this.noEdit) {
      this.map.addListener('click', (event: any) => {
        if (event?.latLng) {
          this._zone.run(() => this.selectLocation(event.latLng.lat(), event.latLng.lng(), false));
        }
      });
    }

    this.watchTheme();
    this.mapReady = true;

    if (this.useCurrentLocation && !this.noEdit && !this.hasLocation) {
      this.locateMe();
    }
  }

  private selectLocation(lat: number, lng: number, zoomIn: boolean) {
    if (!MapPickerComponent.isValid(lat, lng)) return;

    this.lat = lat;
    this.lng = lng;
    this.locateError = '';
    this.placeMarker(lat, lng, zoomIn);
    this.locationSelected.emit({ latitude: lat, longitude: lng, timestamp: new Date().toISOString() });
  }

  private placeMarker(lat: number, lng: number, recenter: boolean) {
    const position = { lat, lng };
    if (this.marker) {
      this.marker.setPosition(position);
    } else {
      this.createMarker(position);
    }

    if (recenter) {
      this.map.panTo(position);
      if (this.map.getZoom() < PIN_ZOOM - 1) this.map.setZoom(PIN_ZOOM);
    }
  }

  private markerIsAt(lat: number, lng: number): boolean {
    const position = this.marker?.getPosition();
    return !!position && Math.abs(position.lat() - lat) < 1e-9 && Math.abs(position.lng() - lng) < 1e-9;
  }

  private createMarker(position: { lat: number; lng: number }) {
    this.marker = new google.maps.Marker({
      position,
      map: this.map,
      draggable: !this.noEdit,
      icon: this.pinIcon(MapPickerComponent.isDarkTheme()),
      title: this.noEdit ? 'Station location' : 'Drag to adjust the location'
    });

    if (!this.noEdit) {
      this.marker.addListener('dragend', (event: any) => {
        if (event?.latLng) {
          this._zone.run(() => this.selectLocation(event.latLng.lat(), event.latLng.lng(), false));
        }
      });
    }
  }

  private pinIcon(dark: boolean) {
    return {
      url: stationPinUrl(dark),
      scaledSize: new google.maps.Size(40, 52),
      anchor: new google.maps.Point(20, 51)
    };
  }

  // The dashboard toggles dark mode by adding `dark` to <body>.
  private watchTheme() {
    this.themeObserver = new MutationObserver(() => {
      const dark = MapPickerComponent.isDarkTheme();
      this.map?.setOptions({ styles: dark ? DARK_MAP_STYLES : LIGHT_MAP_STYLES });
      this.marker?.setIcon(this.pinIcon(dark));
    });
    this.themeObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });
  }

  private static isDarkTheme(): boolean {
    return document.body.classList.contains('dark');
  }

  private static isValid(lat: number | null | undefined, lng: number | null | undefined): boolean {
    return lat != null && lng != null && isFinite(lat) && isFinite(lng)
      && Math.abs(lat) <= 90 && Math.abs(lng) <= 180 && !(lat === 0 && lng === 0);
  }
}
