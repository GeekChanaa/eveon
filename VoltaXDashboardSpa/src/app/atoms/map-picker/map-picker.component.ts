import { Component, ElementRef,  OnInit,  ViewChild, AfterViewInit, Input, EventEmitter, Output  } from '@angular/core';
/// <reference types="@types/googlemaps" />
declare var google: any;

@Component({
  selector: 'app-map-picker',
  templateUrl: './map-picker.component.html',
  styleUrls: ['./map-picker.component.sass']
})
export class MapPickerComponent implements OnInit, AfterViewInit {
  @ViewChild('mapContainer', { static: false }) gmap!: ElementRef;

  map!: google.maps.Map;
  @Input() lat = 0; // Default latitude
  @Input() lng = 0; // Default longitude

  @Input() noEdit: boolean = false;

  @Output() locationSelected: EventEmitter<any> = new EventEmitter<any>();

  coordinates!: google.maps.LatLng;

  mapOptions!: google.maps.MapOptions;

  marker!: google.maps.Marker;

  locationChosen: boolean = false; // To track if a location has been chosen

  ngOnInit() {
    this.coordinates = new google.maps.LatLng(this.lat, this.lng);

    this.mapOptions = {
      center: this.coordinates,
      zoom: 18,
    };
  }

  ngAfterViewInit() {
    if (this.noEdit) {
      this.mapInitializerNoEdit();
    } else {
      this.mapInitializer();
    }
  }

  mapInitializer() {
    this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);

    this.marker = new google.maps.Marker({
      position: this.coordinates,
      map: this.map,
      draggable: true // make the marker draggable
    });

    // Listen for drag events on the marker
    this.marker.addListener('dragend', (event: any) => {
      this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
    });

    // Listen for double-click events to select location
    this.map.addListener('dblclick', (event: google.maps.MapMouseEvent) => {
      this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
    });

    // Listen for single click to place the marker
    this.map.addListener('click', (event: google.maps.MapMouseEvent) => {
      this.handleLocationSelection(event.latLng.lat(), event.latLng.lng());
    });
  }

  mapInitializerNoEdit() {
    this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);

    this.marker = new google.maps.Marker({
      position: this.coordinates,
      map: this.map
    });
  }

  handleLocationSelection(lat: number, lng: number) {
    // Update marker position
    this.marker.setPosition(new google.maps.LatLng(lat, lng));

    // Emit the selected location
    this.locationSelected.emit({ latitude: lat, longitude: lng });

    // Mark location as chosen
    this.locationChosen = true;
  }

}
