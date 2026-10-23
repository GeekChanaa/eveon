import { Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { StationOpeningHours, StationOpeningPeriod } from 'src/_models/portal';
import { LanguageService } from 'src/_services/language.service';

/** ISO weekdays, Monday (1) first. */
export const WEEKDAYS = [1, 2, 3, 4, 5, 6, 7];

/** Name of an ISO weekday in the given locale (1 = Monday). */
export function weekdayName(weekday: number, locale: string, style: 'long' | 'short' = 'long'): string {
  // 1 January 2024 was a Monday.
  return new Intl.DateTimeFormat(locale, { weekday: style }).format(new Date(2024, 0, weekday));
}

interface DayModel {
  weekday: number;
  open: boolean;
  periods: { from: string; to: string }[];
}

/** The periods of each weekday, closed days included, for display. */
export function hoursByDay(hours: StationOpeningHours): { weekday: number; periods: StationOpeningPeriod[] }[] {
  return WEEKDAYS.map(weekday => ({ weekday, periods: hours.regular.filter(p => p.weekday === weekday) }));
}

/**
 * Edits a station's weekly opening hours: open around the clock, or periods per weekday (time inputs; an end at
 * 00:00 means midnight, stored as 24:00). Emits the hours on every change; the API validates them again.
 */
@Component({
  selector: 'app-opening-hours-editor',
  template: `
    <fieldset class="vx-hours" [attr.aria-describedby]="'hours-hint'">
      <legend class="vx-sr-only">{{ 'portal.station.hours.title' | translate }}</legend>
      <div class="vx-checks" role="radiogroup">
        <label class="vx-check"><input type="radio" name="hoursMode" [checked]="alwaysOpen" (change)="setAlwaysOpen(true)">
          {{ 'portal.station.hours.alwaysOpen' | translate }}</label>
        <label class="vx-check"><input type="radio" name="hoursMode" [checked]="!alwaysOpen" (change)="setAlwaysOpen(false)">
          {{ 'portal.station.hours.regular' | translate }}</label>
      </div>
      <p id="hours-hint" class="vx-field__hint">{{ 'portal.station.hours.hint' | translate }}</p>

      <ng-container *ngIf="!alwaysOpen">
        <div class="vx-hours__day" *ngFor="let day of days">
          <label class="vx-check"><input type="checkbox" [checked]="day.open" (change)="toggle(day, $any($event.target).checked)">
            <strong>{{ dayName(day.weekday) }}</strong></label>
          <div class="vx-hours__periods">
            <span class="vx-field__hint" *ngIf="!day.open">{{ 'portal.station.hours.closed' | translate }}</span>
            <div class="vx-hours__period" *ngFor="let period of day.periods; let i = index">
              <label class="vx-sr-only" [for]="'from-' + day.weekday + '-' + i">{{ 'portal.station.hours.from' | translate }}</label>
              <input type="time" [id]="'from-' + day.weekday + '-' + i" [value]="period.from" (change)="period.from = $any($event.target).value; emit()" required>
              <span aria-hidden="true">–</span>
              <label class="vx-sr-only" [for]="'to-' + day.weekday + '-' + i">{{ 'portal.station.hours.to' | translate }}</label>
              <input type="time" [id]="'to-' + day.weekday + '-' + i" [value]="period.to" (change)="period.to = $any($event.target).value; emit()" required>
              <button type="button" class="vx-btn vx-btn--small" *ngIf="day.periods.length > 1" (click)="removePeriod(day, i)"
                [attr.aria-label]="'portal.station.hours.removePeriod' | translate">×</button>
            </div>
            <div class="vx-actions" *ngIf="day.open">
              <button type="button" class="vx-btn vx-btn--small vx-btn--link" (click)="addPeriod(day)" *ngIf="day.periods.length < 3">
                {{ 'portal.station.hours.addPeriod' | translate }}</button>
              <button type="button" class="vx-btn vx-btn--small vx-btn--link" (click)="copyToAll(day)" *ngIf="day.weekday === 1">
                {{ 'portal.station.hours.copyToAll' | translate }}</button>
            </div>
          </div>
        </div>
        <p class="vx-field__error" *ngIf="problem" role="alert">{{ problem | translate }}</p>
      </ng-container>
    </fieldset>`
})
export class OpeningHoursEditorComponent implements OnChanges {
  private readonly language = inject(LanguageService);

  @Input() hours: StationOpeningHours = { twentyFourSeven: true, regular: [] };
  @Output() hoursChange = new EventEmitter<StationOpeningHours>();

  alwaysOpen = true;
  days: DayModel[] = [];

  ngOnChanges(): void {
    this.alwaysOpen = this.hours.twentyFourSeven;
    this.days = WEEKDAYS.map(weekday => {
      const periods = this.hours.regular.filter(p => p.weekday === weekday)
        .map(p => ({ from: p.periodBegin, to: p.periodEnd === '24:00' ? '00:00' : p.periodEnd }));
      return { weekday, open: periods.length > 0, periods };
    });
  }

  dayName(weekday: number): string { return weekdayName(weekday, this.language.locale); }

  setAlwaysOpen(value: boolean): void {
    this.alwaysOpen = value;
    // Switching to regular hours starts from a usual week (Monday to Saturday 08:00–20:00).
    if (!value && !this.days.some(d => d.open))
      this.days.forEach(d => { d.open = d.weekday <= 6; d.periods = d.open ? [{ from: '08:00', to: '20:00' }] : []; });
    this.emit();
  }

  toggle(day: DayModel, open: boolean): void {
    day.open = open;
    day.periods = open ? (day.periods.length ? day.periods : [{ from: '08:00', to: '20:00' }]) : [];
    this.emit();
  }

  addPeriod(day: DayModel): void {
    const last = day.periods[day.periods.length - 1];
    day.periods.push({ from: last?.to && last.to !== '00:00' ? last.to : '14:00', to: '00:00' });
    this.emit();
  }

  removePeriod(day: DayModel, index: number): void {
    day.periods.splice(index, 1);
    this.emit();
  }

  copyToAll(source: DayModel): void {
    this.days.forEach(day => {
      day.open = source.open;
      day.periods = source.periods.map(p => ({ ...p }));
    });
    this.emit();
  }

  /** The hours in the API's shape (an end at 00:00 is midnight: 24:00). */
  get value(): StationOpeningHours {
    if (this.alwaysOpen) return { twentyFourSeven: true, regular: [] };
    return {
      twentyFourSeven: false,
      regular: this.days.filter(d => d.open).flatMap(d => d.periods.map(p => ({
        weekday: d.weekday, periodBegin: p.from, periodEnd: p.to === '00:00' ? '24:00' : p.to
      })))
    };
  }

  /** What is wrong with the hours as typed, as a translation key (null when fine). */
  get problem(): string | null {
    const value = this.value;
    if (value.twentyFourSeven) return null;
    if (!value.regular.length) return 'portal.station.hours.noneOpen';
    if (value.regular.some(p => !p.periodBegin || !p.periodEnd || p.periodEnd <= p.periodBegin)) return 'portal.station.hours.endAfterStart';
    for (const weekday of WEEKDAYS) {
      const periods = value.regular.filter(p => p.weekday === weekday).sort((a, b) => a.periodBegin.localeCompare(b.periodBegin));
      if (periods.some((p, i) => i > 0 && p.periodBegin < periods[i - 1].periodEnd)) return 'portal.station.hours.overlap';
    }
    return null;
  }

  emit(): void { this.hoursChange.emit(this.value); }
}
