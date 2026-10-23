import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerChargePoint, PartnerConnector } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PartnerChargePointsComponent } from './partner-charge-points.component';

const connector = (id: number): PartnerConnector => ({
  id, evseId: id, connectorId: 1, type: 'cType2', powerKw: 22, status: 'Available',
  pricePerKWh: 2, pricePerMinute: 0, pricePerIdleMinute: 0, flatFee: 0
});

describe('PartnerChargePointsComponent', () => {
  let portal: jasmine.SpyObj<PartnerPortalService>;
  let point: PartnerChargePoint;
  let component: PartnerChargePointsComponent;
  const pricing = { pricePerKWh: 3, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, flatFee: 1 };

  beforeEach(async () => {
    point = { id: 1, chargePointId: 'CP-1', stationId: 1, stationName: 'Marina', vendor: null, model: null, status: 'Available', connectors: [connector(10), connector(11)] };
    portal = jasmine.createSpyObj<PartnerPortalService>('PartnerPortalService', ['stations', 'chargePoints', 'updatePricing']);
    portal.stations.and.returnValue(of([]));
    portal.chargePoints.and.returnValue(of([point]));
    portal.updatePricing.and.callFake((id: number, p) => of({ ...connector(id), ...p }));
    await configureVxTest([PartnerChargePointsComponent], { providers: [{ provide: PartnerPortalService, useValue: portal }] });
    const fixture = TestBed.createComponent(PartnerChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('lists the partner charge points', () => {
    expect(component.chargePoints).toEqual([point]);
    expect(portal.chargePoints).toHaveBeenCalledWith(null);
  });

  it('saves one connector and keeps the others unchanged', () => {
    component.edit(point.connectors[0]);
    component.save(point, point.connectors[0], { pricing, applyToAll: false });

    expect(portal.updatePricing).toHaveBeenCalledOnceWith(10, pricing);
    expect(point.connectors[0].pricePerKWh).toBe(3);
    expect(point.connectors[1].pricePerKWh).toBe(2);
    expect(component.editing).toBeNull();
    expect(component.savedConnectors.has(10)).toBeTrue();
  });

  it('applies the prices to every connector when asked', () => {
    component.save(point, point.connectors[0], { pricing, applyToAll: true });

    expect(portal.updatePricing.calls.allArgs()).toEqual([[10, pricing], [11, pricing]]);
    expect(point.connectors.map(c => c.flatFee)).toEqual([1, 1]);
  });

  it('keeps the editor open with a message when the API refuses the prices', () => {
    portal.updatePricing.and.returnValue(throwError(() => ({ status: 400 })));
    component.edit(point.connectors[0]);
    component.save(point, point.connectors[0], { pricing, applyToAll: false });

    expect(component.editing).toBe(10);
    expect(component.saveError).toBe('partner.pricing.invalid');
    expect(point.connectors[0].pricePerKWh).toBe(2);
  });

  it('labels EVSEs with one connector by their EVSE only', () => {
    expect(component.connectorLabel(connector(3))).toBe('EVSE 3');
    expect(component.connectorLabel({ ...connector(3), connectorId: 2 })).toBe('EVSE 3 / 2');
  });
});
