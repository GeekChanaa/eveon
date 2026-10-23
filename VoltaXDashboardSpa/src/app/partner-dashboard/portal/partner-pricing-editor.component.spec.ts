import { ComponentFixture, TestBed } from '@angular/core/testing';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerConnector } from 'src/_models/portal';
import { PartnerPricingEditorComponent, PricingChange } from './partner-pricing-editor.component';

const connector: PartnerConnector = {
  id: 4, evseId: 1, connectorId: 1, type: 'cCCS2', powerKw: 50, status: 'Available',
  pricePerKWh: 3.5, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, flatFee: 2
};

describe('PartnerPricingEditorComponent', () => {
  let fixture: ComponentFixture<PartnerPricingEditorComponent>;
  let editor: PartnerPricingEditorComponent;
  let saved: PricingChange[];

  beforeEach(async () => {
    await configureVxTest([PartnerPricingEditorComponent]);
    fixture = TestBed.createComponent(PartnerPricingEditorComponent);
    editor = fixture.componentInstance;
    editor.connector = connector;
    editor.siblings = 1;
    editor.ngOnChanges();
    saved = [];
    editor.save.subscribe(change => saved.push(change));
    fixture.detectChanges();
  });

  it('starts from the connector prices and shows what a typical session costs', () => {
    expect(editor.pricing).toEqual({ pricePerKWh: 3.5, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, flatFee: 2 });
    expect(editor.example).toBeCloseTo(2 + 20 * 3.5 + 60 * 0.1, 6);
  });

  it('accepts a decimal comma and emits numbers', () => {
    editor.form.controls.pricePerKWh.setValue('4,25');
    editor.submit();

    expect(saved.length).toBe(1);
    expect(saved[0].pricing.pricePerKWh).toBe(4.25);
    expect(saved[0].applyToAll).toBeFalse();
  });

  it('refuses prices outside the API ranges or with more than 4 decimals', () => {
    for (const [key, value] of [['pricePerKWh', '1001'], ['pricePerMinute', '100.5'], ['flatFee', '-1'], ['pricePerIdleMinute', '0.12345'], ['pricePerKWh', 'abc']] as const) {
      editor.ngOnChanges();
      editor.form.controls[key].setValue(value);
      editor.submit();
    }

    expect(saved).toEqual([]);
  });

  it('can apply the prices to every connector of the charge point', () => {
    editor.applyToAll = true;
    editor.submit();

    expect(saved[0].applyToAll).toBeTrue();
  });

  it('offers "apply to all" only when the charge point has other connectors', () => {
    expect((fixture.nativeElement as HTMLElement).querySelector('.vx-check')).not.toBeNull();
    editor.siblings = 0;
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.vx-check')).toBeNull();
  });
});
