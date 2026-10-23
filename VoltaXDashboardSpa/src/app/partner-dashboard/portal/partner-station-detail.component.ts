import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import {
  PartnerChargePoint, PartnerStationDetail, PartnerStationPhoto, StationAccess, StationAmenities, StationOpeningHours, StationParkingType
} from 'src/_models/portal';
import { LanguageService } from 'src/_services/language.service';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { VxConfirmDialogComponent, connectorTypeLabel, statusTone } from 'src/app/shared/portal-ui/portal-ui.module';
import { OpeningHoursEditorComponent, hoursByDay, weekdayName } from './opening-hours';

const PHOTO_TYPES = ['image/png', 'image/jpeg', 'image/webp'];
const PHOTO_MAX_BYTES = 5 * 1024 * 1024;
export const MAX_PHOTOS = 8;
export const MAX_PHOTOS_PER_UPLOAD = 5;

export const AMENITIES: (keyof StationAmenities)[] = ['wifi', 'parking', 'restaurants', 'washroom', 'sittingArea'];
export const PARKING_TYPES: StationParkingType[] = ['ParallelParking', 'PerpendicularParking', 'AngleParking'];

/** Latitude and longitude go together. */
function bothOrNeither(group: FormGroup): ValidationErrors | null {
  const lat = group.get('latitude')?.value, lng = group.get('longitude')?.value;
  const given = (v: unknown) => v !== null && v !== undefined && v !== '';
  return given(lat) !== given(lng) ? { coordinates: true } : null;
}

/**
 * One of the partner's stations as drivers see it: address and location, access, opening hours, description,
 * amenities and photos, which the partner can change, and its charge points with their prices. Printable.
 */
@Component({
  selector: 'app-partner-station-detail',
  templateUrl: './partner-station-detail.component.html'
})
export class PartnerStationDetailComponent implements OnInit {
  private readonly portal = inject(PartnerPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('removePhotoDialog') removePhotoDialog?: VxConfirmDialogComponent;
  @ViewChild(OpeningHoursEditorComponent) hoursEditor?: OpeningHoursEditorComponent;

  station: PartnerStationDetail | null = null;
  chargePoints: PartnerChargePoint[] = [];
  notFound = false;
  editing = false;
  saving = false;
  saved = false;
  saveError: string | null = null;
  nameTaken = false;
  /** Opening hours being edited (the editor's last output). */
  hours: StationOpeningHours = { twentyFourSeven: true, regular: [] };
  uploading = false;
  photoError: string | null = null;
  photoToRemove: PartnerStationPhoto | null = null;
  readonly amenities = AMENITIES;
  readonly parkingTypes = PARKING_TYPES;
  readonly tone = statusTone;
  readonly typeLabel = connectorTypeLabel;
  readonly fileUrl = PartnerPortalService.fileUrl;
  readonly maxPhotos = MAX_PHOTOS;

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2), Validators.maxLength(100)] }),
    address: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(3), Validators.maxLength(200)] }),
    city: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2), Validators.maxLength(100)] }),
    latitude: new FormControl<number | null>(null, [Validators.min(-90), Validators.max(90)]),
    longitude: new FormControl<number | null>(null, [Validators.min(-180), Validators.max(180)]),
    access: new FormControl<StationAccess>('Public', { nonNullable: true, validators: Validators.required }),
    parkingType: new FormControl<StationParkingType>('PerpendicularParking', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true, validators: Validators.maxLength(1000) }),
    wifi: new FormControl(false, { nonNullable: true }),
    parking: new FormControl(false, { nonNullable: true }),
    restaurants: new FormControl(false, { nonNullable: true }),
    washroom: new FormControl(false, { nonNullable: true }),
    sittingArea: new FormControl(false, { nonNullable: true })
  }, { validators: c => bothOrNeither(c as FormGroup) });

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => Number(params.get('id'))),
      switchMap(id => this.portal.station(id).pipe(catchError(() => of(null)))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(station => {
      this.notFound = station === null;
      if (station) this.show(station);
      if (station) this.portal.chargePoints(station.id).pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: points => this.chargePoints = points, error: () => this.chargePoints = [] });
    });
  }

  get days() { return this.station ? hoursByDay(this.station.openingHours) : []; }

  dayName(weekday: number): string { return weekdayName(weekday, this.language.locale); }

  get descriptionLength(): number { return this.form.controls.description.value.length; }

  startEditing(): void {
    if (!this.station) return;
    const s = this.station;
    this.form.reset({
      name: s.name, address: s.address, city: s.city, latitude: s.latitude, longitude: s.longitude, access: s.access,
      parkingType: s.parkingType, description: s.description ?? '', ...s.amenities
    });
    this.hours = s.openingHours;
    this.saved = false;
    this.saveError = null;
    this.nameTaken = false;
    this.editing = true;
  }

  cancel(): void { this.editing = false; }

  save(): void {
    if (!this.station) return;
    if (this.form.invalid || this.hoursEditor?.problem) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    this.saving = true;
    this.saveError = null;
    this.nameTaken = false;
    this.portal.updateStation(this.station.id, {
      name: v.name.trim(), address: v.address.trim(), city: v.city.trim(),
      latitude: v.latitude === null || (v.latitude as unknown) === '' ? null : Number(v.latitude),
      longitude: v.longitude === null || (v.longitude as unknown) === '' ? null : Number(v.longitude),
      access: v.access, parkingType: v.parkingType, description: v.description.trim() || null,
      openingHours: this.hoursEditor?.value ?? this.hours,
      amenities: { wifi: v.wifi, parking: v.parking, restaurants: v.restaurants, washroom: v.washroom, sittingArea: v.sittingArea }
    }).subscribe({
      next: station => { this.show(station); this.saving = false; this.editing = false; this.saved = true; },
      error: (error: HttpErrorResponse) => {
        this.saving = false;
        this.nameTaken = error.status === 409;
        this.saveError = error.status === 409 ? 'portal.station.nameTaken' : error.status === 400 ? 'portal.station.invalid' : 'portal.station.saveFailed';
      }
    });
  }

  // ------------------------------------------------------------------ photos

  addPhotos(files: FileList | null): void {
    this.photoError = null;
    const chosen = Array.from(files ?? []);
    if (!this.station || !chosen.length) return;
    if (chosen.some(f => !PHOTO_TYPES.includes(f.type))) { this.photoError = 'portal.station.photos.badType'; return; }
    if (chosen.some(f => f.size > PHOTO_MAX_BYTES)) { this.photoError = 'portal.station.photos.tooBig'; return; }
    if (chosen.length > MAX_PHOTOS_PER_UPLOAD) { this.photoError = 'portal.station.photos.tooManyAtOnce'; return; }
    if (this.station.photos.length + chosen.length > MAX_PHOTOS) { this.photoError = 'portal.station.photos.tooMany'; return; }
    this.uploading = true;
    this.portal.addStationPhotos(this.station.id, chosen).subscribe({
      next: station => { this.station = station; this.uploading = false; },
      error: () => { this.uploading = false; this.photoError = 'portal.station.photos.uploadFailed'; }
    });
  }

  askRemovePhoto(photo: PartnerStationPhoto): void {
    this.photoToRemove = photo;
    this.removePhotoDialog?.open();
  }

  removePhoto(): void {
    const photo = this.photoToRemove;
    this.photoToRemove = null;
    if (!this.station || !photo) return;
    const stationId = this.station.id;
    this.portal.removeStationPhoto(stationId, photo.id).subscribe({
      next: () => { if (this.station?.id === stationId) this.station = { ...this.station, photos: this.station.photos.filter(p => p.id !== photo.id) }; },
      error: () => this.photoError = 'portal.station.photos.removeFailed'
    });
  }

  print(): void { window.print(); }

  connectorLabel(evseId: number, connectorId: number | null): string {
    return connectorId == null || connectorId === 1 ? `EVSE ${evseId}` : `EVSE ${evseId} / ${connectorId}`;
  }

  mapUrl(s: PartnerStationDetail): string | null {
    return s.latitude != null && s.longitude != null ? `https://www.google.com/maps/search/?api=1&query=${s.latitude},${s.longitude}` : null;
  }

  private show(station: PartnerStationDetail): void {
    this.station = station;
    this.hours = station.openingHours;
  }
}
