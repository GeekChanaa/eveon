import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ConnectorPricing, PartnerConnector } from 'src/_models/portal';

const PRICE = /^\d+([.,]\d{1,4})?$/;

function priceControl(value: number, max: number): FormControl<string> {
  return new FormControl(String(value ?? 0), { nonNullable: true, validators: [Validators.required, Validators.pattern(PRICE), Validators.min(0), Validators.max(max)] });
}

export interface PricingChange { pricing: ConnectorPricing; applyToAll: boolean; }

/**
 * Edits the four prices of a connector (VAT included). Saving is left to the parent, which may apply the prices
 * to every connector of the charge point.
 */
@Component({
  selector: 'app-partner-pricing-editor',
  template: `
    <form class="vx-pricing" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="vx-pricing__fields">
        <div class="vx-field" *ngFor="let f of fields">
          <label [for]="f.key + connector.id">{{ f.label | translate }}</label>
          <div class="vx-input-unit">
            <input [id]="f.key + connector.id" [formControlName]="f.key" inputmode="decimal" autocomplete="off">
            <span>{{ 'common.currency.mad' | translate }}{{ f.unit ? ' ' + (f.unit | translate) : '' }}</span>
          </div>
          <small class="vx-field__error" *ngIf="form.controls[f.key].touched && form.controls[f.key].invalid">
            {{ 'partner.pricing.range' | translate:{ max: f.max } }}
          </small>
        </div>
      </div>
      <p class="vx-pricing__example" *ngIf="form.valid">
        {{ 'partner.pricing.example' | translate:{ kwh: 20, minutes: 60, amount: (example | vxMoney) } }}
      </p>
      <p class="vx-pricing__note">{{ 'partner.pricing.note' | translate }}</p>
      <div class="vx-pricing__actions">
        <label class="vx-check" *ngIf="siblings > 0">
          <input type="checkbox" [checked]="applyToAll" (change)="applyToAll = $any($event.target).checked">
          {{ 'partner.pricing.applyToAll' | translate:{ count: siblings + 1 } }}
        </label>
        <span class="vx-actions">
          <button type="button" class="vx-btn" (click)="cancel.emit()" [disabled]="saving">{{ 'common.actions.cancel' | translate }}</button>
          <button type="submit" class="vx-btn vx-btn--primary" [disabled]="saving || form.pristine && !applyToAll">
            {{ (saving ? 'partner.pricing.saving' : 'partner.pricing.save') | translate }}
          </button>
        </span>
      </div>
    </form>`
})
export class PartnerPricingEditorComponent implements OnChanges {
  @Input() connector!: PartnerConnector;
  /** Other connectors on the same charge point (offers "apply to all" when there are some). */
  @Input() siblings = 0;
  @Input() saving = false;
  @Output() save = new EventEmitter<PricingChange>();
  @Output() cancel = new EventEmitter<void>();

  applyToAll = false;
  form = this.build(null);

  readonly fields: { key: keyof ConnectorPricing; label: string; unit: string | null; max: number }[] = [
    { key: 'pricePerKWh', label: 'partner.pricing.perKwh', unit: 'common.units.perKwh', max: 1000 },
    { key: 'pricePerMinute', label: 'partner.pricing.perMinute', unit: 'common.units.perMin', max: 100 },
    { key: 'pricePerIdleMinute', label: 'partner.pricing.perIdleMinute', unit: 'common.units.perMin', max: 100 },
    { key: 'flatFee', label: 'partner.pricing.flatFee', unit: null, max: 1000 }
  ];

  ngOnChanges(): void {
    this.form = this.build(this.connector);
    this.applyToAll = false;
  }

  /** The prices as numbers (the inputs accept a decimal comma). */
  get pricing(): ConnectorPricing {
    const v = this.form.getRawValue();
    const n = (s: string) => Number(String(s).replace(',', '.'));
    return { pricePerKWh: n(v.pricePerKWh), pricePerMinute: n(v.pricePerMinute), pricePerIdleMinute: n(v.pricePerIdleMinute), flatFee: n(v.flatFee) };
  }

  /** What a 20 kWh, 60 minute session would cost with these prices (idle fee aside). */
  get example(): number {
    const p = this.pricing;
    return p.flatFee + 20 * p.pricePerKWh + 60 * p.pricePerMinute;
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.save.emit({ pricing: this.pricing, applyToAll: this.applyToAll });
  }

  private build(c: PartnerConnector | null) {
    return new FormGroup({
      pricePerKWh: priceControl(c?.pricePerKWh ?? 0, 1000),
      pricePerMinute: priceControl(c?.pricePerMinute ?? 0, 100),
      pricePerIdleMinute: priceControl(c?.pricePerIdleMinute ?? 0, 100),
      flatFee: priceControl(c?.flatFee ?? 0, 1000)
    });
  }
}
