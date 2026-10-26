import { Component, EventEmitter, Input, OnChanges, OnInit, Output } from '@angular/core';
import { catchError, of, switchMap } from 'rxjs';
import { MyProfile } from 'src/_models/portal';
import { CityService } from 'src/_services/city.service';
import { ElectricVehicleModelService } from 'src/_services/electric-vehicle-model.service';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { ProfileCompletionService } from 'src/_services/portal/profile-completion.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

interface Option<T> { value: T; label: string; }

/**
 * The optional details of the signed-in customer: date of birth, gender, city and vehicle. Nothing here is
 * required; with skippable set, a Skip button lets the customer leave without saving.
 */
@Component({
  selector: 'app-customer-details-form',
  standalone: true,
  imports: [PortalUiModule],
  template: `
<form (ngSubmit)="save()">
  <div class="vx-form-grid">
    <div class="vx-field">
      <label [for]="id + '-birthday'">{{ 'customer.profile.birthday' | translate }}</label>
      <input [id]="id + '-birthday'" name="birthday" type="date" [max]="today" min="1900-01-01" [(ngModel)]="birthday">
    </div>
    <div class="vx-field">
      <label [for]="id + '-gender'">{{ 'portal.completion.gender' | translate }}</label>
      <select [id]="id + '-gender'" name="gender" [(ngModel)]="gender">
        <option [ngValue]="null">{{ 'portal.completion.notSaid' | translate }}</option>
        <option ngValue="Female">{{ 'portal.completion.female' | translate }}</option>
        <option ngValue="Male">{{ 'portal.completion.male' | translate }}</option>
      </select>
    </div>
    <div class="vx-field">
      <label [for]="id + '-city'">{{ 'portal.completion.city' | translate }}</label>
      <select [id]="id + '-city'" name="city" [(ngModel)]="city">
        <option [ngValue]="null">{{ 'portal.completion.notSaid' | translate }}</option>
        <option *ngFor="let c of cities" [ngValue]="c.value">{{ c.label }}</option>
      </select>
    </div>
    <div class="vx-field">
      <label [for]="id + '-vehicle'">{{ 'portal.completion.vehicle' | translate }}</label>
      <select [id]="id + '-vehicle'" name="vehicle" [(ngModel)]="vehicleId">
        <option [ngValue]="null">{{ 'portal.completion.noVehicle' | translate }}</option>
        <option *ngFor="let v of vehicles" [ngValue]="v.value">{{ v.label }}</option>
      </select>
    </div>
  </div>
  <div class="vx-alert vx-alert--error" *ngIf="error" role="alert" style="margin-block: 14px 0">{{ 'portal.completion.saveFailed' | translate }}</div>
  <div class="vx-alert vx-alert--success" *ngIf="saved && !skippable" role="status" style="margin-block: 14px 0">{{ 'portal.completion.saved' | translate }}</div>
  <div class="vx-actions" style="margin-block-start: 16px">
    <button type="submit" class="vx-btn vx-btn--primary" [disabled]="saving">{{ 'common.actions.save' | translate }}</button>
    <button type="button" class="vx-btn" *ngIf="skippable" (click)="skipped.emit()">{{ 'portal.completion.skip' | translate }}</button>
  </div>
</form>`
})
export class CustomerDetailsFormComponent implements OnInit, OnChanges {
  private static nextId = 0;
  @Input() profile: MyProfile | null = null;
  @Input() skippable = false;
  @Output() done = new EventEmitter<void>();
  @Output() skipped = new EventEmitter<void>();

  readonly id = `vx-details-${++CustomerDetailsFormComponent.nextId}`;
  readonly today = new Date().toISOString().slice(0, 10);
  birthday: string | null = null;
  gender: 'Male' | 'Female' | null = null;
  city: string | null = null;
  vehicleId: number | null = null;
  cities: Option<string>[] = [];
  vehicles: Option<number>[] = [];
  saving = false;
  saved = false;
  error = false;

  constructor(private portal: CustomerPortalService, private completion: ProfileCompletionService,
    private citiesApi: CityService, private vehiclesApi: ElectricVehicleModelService) {}

  ngOnInit(): void {
    this.citiesApi.getAllMoroccoCityNames().pipe(catchError(() => of([]))).subscribe(cities => {
      this.cities = cities.map((c: { name: string }) => ({ value: c.name, label: c.name })).sort((a, b) => a.label.localeCompare(b.label));
      this.keepCurrentCity();
    });
    this.vehiclesApi.getAllElectricVehicleModelsForSelect().pipe(catchError(() => of([]))).subscribe(models => {
      this.vehicles = models.map((m: { id: number; make: string; model: string }) => ({ value: m.id, label: `${m.make} ${m.model}` }))
        .sort((a, b) => a.label.localeCompare(b.label));
    });
  }

  ngOnChanges(): void {
    const p = this.profile;
    this.birthday = p?.birthday ? p.birthday.slice(0, 10) : null;
    this.gender = p?.gender ?? null;
    this.city = p?.city ?? null;
    this.vehicleId = p?.electricVehicleModelId ?? null;
    this.keepCurrentCity();
  }

  save(): void {
    this.saving = true;
    this.saved = false;
    this.error = false;
    this.portal.updateProfile({ birthday: this.birthday || null, gender: this.gender, city: this.city, electricVehicleModelId: this.vehicleId })
      .pipe(switchMap(() => this.completion.load()))
      .subscribe({
        next: () => { this.saving = false; this.saved = true; this.done.emit(); },
        error: () => { this.saving = false; this.error = true; }
      });
  }

  /** A city saved before (or from the app) that is not in the list stays selectable. */
  private keepCurrentCity(): void {
    if (this.city && this.cities.length && !this.cities.some(c => c.value === this.city))
      this.cities = [{ value: this.city, label: this.city }, ...this.cities];
  }
}
