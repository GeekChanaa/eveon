import { Component, ElementRef, OnInit, ViewChild, AfterViewInit, Input, EventEmitter, Output, OnDestroy } from '@angular/core';

// Declare google as any to avoid TypeScript errors
declare const google: any;

@Component({
  selector: 'app-map-picker',
  templateUrl: './map-picker.component.html',
  styleUrls: ['./map-picker.component.sass']
})
export class MapPickerComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('mapContainer', { static: false }) gmap!: ElementRef;
  
  map: any;
  @Input() lat = 0; // Default latitude
  @Input() lng = 0; // Default longitude
  @Input() noEdit: boolean = false;
  @Output() locationSelected: EventEmitter<any> = new EventEmitter<any>();
  @Input() useCurrentLocation: boolean = false;
  
  coordinates: any;
  mapOptions: any;
  marker: any;
  locationChosen: boolean = false;
  private mapInitialized = false;
  private isDestroyed = false;

  ngOnInit() {
    this.initializeCoordinates();
  }

  ngAfterViewInit() {
    // Small delay to ensure DOM is ready
    setTimeout(() => {
      if (!this.isDestroyed) {
        this.initializeMap();
      }
    }, 100);
  }

  ngOnDestroy() {
    this.isDestroyed = true;
    this.cleanup();
  }

  private initializeCoordinates() {
    try {
      this.coordinates = new google.maps.LatLng(this.lat, this.lng);
      this.mapOptions = {
        center: this.coordinates,
        zoom: 18,
        mapTypeId: google.maps.MapTypeId.ROADMAP
      };
    } catch (error) {
      console.error('Error initializing coordinates:', error);
      this.handleGoogleMapsError();
    }
  }

  private async initializeMap() {
    if (this.mapInitialized || !this.gmap?.nativeElement) {
      return;
    }

    try {
      if (typeof google === 'undefined') {
        throw new Error('Google Maps API not loaded');
      }

      if (this.useCurrentLocation && !this.locationChosen) {
        await this.setCurrentLocation();
      }
      
      if (this.noEdit) {
        this.mapInitializerNoEdit();
      } else {
        this.mapInitializer();
      }
      
      this.mapInitialized = true;
    } catch (error) {
      console.error('Error initializing map:', error);
      this.handleGoogleMapsError();
    }
  }

  private setCurrentLocation(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        console.warn('Geolocation is not supported by this browser.');
        resolve();
        return;
      }

      const options = {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0
      };

      navigator.geolocation.getCurrentPosition(
        (position) => {
          try {
            this.lat = position.coords.latitude;
            this.lng = position.coords.longitude;
            this.coordinates = new google.maps.LatLng(this.lat, this.lng);
            this.mapOptions = {
              center: this.coordinates,
              zoom: 18,
              mapTypeId: google.maps.MapTypeId.ROADMAP
            };
            console.log('Current location set:', this.lat, this.lng);
            resolve();
          } catch (error) {
            console.error('Error processing current location:', error);
            resolve(); // Continue with default location
          }
        },
        (error) => {
          console.warn('Geolocation error:', this.getGeolocationErrorMessage(error));
          resolve(); // Continue with default location
        },
        options
      );
    });
  }

  private getGeolocationErrorMessage(error: GeolocationPositionError): string {
    switch (error.code) {
      case error.PERMISSION_DENIED:
        return 'User denied the request for Geolocation.';
      case error.POSITION_UNAVAILABLE:
        return 'Location information is unavailable.';
      case error.TIMEOUT:
        return 'The request to get user location timed out.';
      default:
        return 'An unknown error occurred.';
    }
  }

  private mapInitializer() {
    try {
      if (!this.gmap?.nativeElement) {
        throw new Error('Map container not found');
      }

      this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);
      
      this.marker = new google.maps.Marker({
        position: this.coordinates,
        map: this.map,
        draggable: true,
        title: 'Drag to select location'
      });

      // Add event listeners
      this.addMapEventListeners();
      
    } catch (error) {
      console.error('Error creating interactive map:', error);
      this.handleGoogleMapsError();
    }
  }

  private mapInitializerNoEdit() {
    try {
      if (!this.gmap?.nativeElement) {
        throw new Error('Map container not found');
      }

      this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);
      
      this.marker = new google.maps.Marker({
        position: this.coordinates,
        map: this.map,
        title: 'Selected location'
      });
      
    } catch (error) {
      console.error('Error creating read-only map:', error);
      this.handleGoogleMapsError();
    }
  }

  private addMapEventListeners() {
    try {
      // Listen for drag events on the marker
      if (this.marker) {
        this.marker.addListener('dragend', (event: any) => {
          if (event?.latLng) {
            this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
          }
        });
      }

      // Listen for map clicks
      if (this.map) {
        this.map.addListener('click', (event: any) => {
          if (event?.latLng) {
            this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
          }
        });

        // Optional: Listen for double-click events
        this.map.addListener('dblclick', (event: any) => {
          if (event?.latLng) {
            this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
          }
        });
      }
    } catch (error) {
      console.error('Error adding map event listeners:', error);
    }
  }

  private handleLocationSelection(lat: number, lng: number) {
    try {
      if (!lat || !lng || isNaN(lat) || isNaN(lng)) {
        console.error('Invalid coordinates provided:', lat, lng);
        return;
      }

      // Update marker position
      if (this.marker) {
        const newPosition = new google.maps.LatLng(lat, lng);
        this.marker.setPosition(newPosition);
      }

      // Update internal coordinates
      this.lat = lat;
      this.lng = lng;
      this.coordinates = new google.maps.LatLng(lat, lng);

      // Emit the selected location
      this.locationSelected.emit({ 
        latitude: lat, 
        longitude: lng,
        timestamp: new Date().toISOString()
      });

      // Mark location as chosen
      this.locationChosen = true;
      
      console.log('Location selected:', lat, lng);
    } catch (error) {
      console.error('Error handling location selection:', error);
    }
  }

  private handleGoogleMapsError() {
    // Emit error event or show user-friendly message
    console.error('Google Maps failed to load. Please check your internet connection and API key.');
    // You could emit an error event here if needed
    // this.mapError.emit('Failed to load Google Maps');
  }

  private cleanup() {
    try {
      // Clean up event listeners and references
      if (this.marker) {
        google.maps.event.clearInstanceListeners(this.marker);
      }
      if (this.map) {
        google.maps.event.clearInstanceListeners(this.map);
      }
    } catch (error) {
      console.error('Error during cleanup:', error);
    }
  }

  // Public method to update map center
  public updateMapCenter(lat: number, lng: number) {
    try {
      if (this.map && lat && lng && !isNaN(lat) && !isNaN(lng)) {
        const newCenter = new google.maps.LatLng(lat, lng);
        this.map.setCenter(newCenter);
        if (this.marker) {
          this.marker.setPosition(newCenter);
        }
        this.lat = lat;
        this.lng = lng;
        this.coordinates = newCenter;
      }
    } catch (error) {
      console.error('Error updating map center:', error);
    }
  }

  // Public method to get current location
  public getCurrentLocation(): { lat: number; lng: number } | null {
    try {
      return this.coordinates ? {
        lat: this.lat,
        lng: this.lng
      } : null;
    } catch (error) {
      console.error('Error getting current location:', error);
      return null;
    }
  }
}