import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerChargePoint, PartnerConnector } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { PartnerChargePointsComponent } from './partner-charge-points.component';

const connector = (id: number, activeSessionId: number | null = null): PartnerConnector => ({
  id, evseId: id, connectorId: 1, type: 'cType2', powerKw: 22, status: 'Available',
  pricePerKWh: 2, pricePerMinute: 0, pricePerIdleMinute: 0, flatFee: 0, activeSessionId
});

describe('PartnerChargePointsComponent', () => {
  let portal: jasmine.SpyObj<PartnerPortalService>;
  let point: PartnerChargePoint;
  let fixture: ComponentFixture<PartnerChargePointsComponent>;
  let component: PartnerChargePointsComponent;
  const pricing = { pricePerKWh: 3, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, flatFee: 1 };

  beforeEach(async () => {
    point = {
      id: 1, chargePointId: 'CP-1', stationId: 1, stationName: 'Marina', vendor: null, model: null, status: 'Available',
      connectors: [connector(10, 77), connector(11)], online: true, protocol: 'ocpp2.0.1'
    };
    portal = jasmine.createSpyObj<PartnerPortalService>('PartnerPortalService',
      ['stations', 'chargePoints', 'updatePricing', 'reset', 'refreshStatus', 'unlock', 'stopSession', 'profile'], { profile$: of(null) });
    portal.stations.and.returnValue(of([]));
    portal.chargePoints.and.returnValue(of([point]));
    portal.profile.and.returnValue(of({ id: 1, name: 'Atlas', email: null, phone: null, city: null, address: null, logoUrl: null, stationCount: 1, chargePointCount: 1 }));
    portal.updatePricing.and.callFake((id: number, p) => of({ ...connector(id), ...p }));
    await configureVxTest([PartnerChargePointsComponent], { imports: [PortalUiModule], providers: [{ provide: PartnerPortalService, useValue: portal }] });
    fixture = TestBed.createComponent(PartnerChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent!;

  it('lists the partner charge points with their connection', () => {
    expect(component.chargePoints).toEqual([point]);
    expect(portal.chargePoints).toHaveBeenCalledWith(null);
    expect(text()).toContain('portal.partner.connected');
    expect(component.protocolLabel('ocpp2.0.1')).toBe('OCPP 2.0.1');
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

  it('asks before a hard restart, warns about the running sessions, then shows the charger\'s answer', fakeAsync(() => {
    portal.reset.and.returnValue(of({ message: 'Reset: charger answered Accepted', status: 'Accepted' }));

    component.ask('hardReset', point);
    fixture.detectChanges();
    expect(component.actionDialog!.isOpen).toBeTrue();
    expect(text()).toContain('portal.partner.confirm.runningWarning');
    expect(portal.reset).not.toHaveBeenCalled();

    (fixture.nativeElement.querySelector('[data-confirm]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(portal.reset).toHaveBeenCalledWith(1, 'Hard');
    expect(component.feedback[1].key).toBe('portal.command.reset.done');
    expect(text()).toContain('portal.command.reset.done');
    tick(4000); // the new connector states are fetched once the charger had time to report them
    expect(portal.chargePoints).toHaveBeenCalledTimes(2);
  }));

  it('sends nothing when the confirmation is cancelled', () => {
    component.ask('softReset', point);
    component.actionDialog!.cancel();

    expect(component.pending).toBeNull();
    expect(portal.reset).not.toHaveBeenCalled();
  });

  it('unlocks a connector and stops the session running on another one', () => {
    portal.unlock.and.returnValue(of({ message: '', status: 'Unlocked' }));
    portal.stopSession.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { status: 'NotRunning' } })));

    component.ask('unlock', point, point.connectors[1]);
    component.confirmPending();
    expect(portal.unlock).toHaveBeenCalledWith(11);
    expect(component.feedback[1].key).toBe('portal.command.unlock.done');

    component.ask('stop', point, point.connectors[0]);
    component.confirmPending();
    expect(portal.stopSession).toHaveBeenCalledWith(77);
    expect(component.feedback[1]).toEqual({ tone: 'warning', key: 'portal.command.notRunning', params: {} });
    expect(component.busy.size).toBe(0);
  });

  it('refreshes the status without confirmation and reports an offline charger', () => {
    portal.refreshStatus.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { status: 'NotConnected' } })));

    component.refresh(point);

    expect(portal.refreshStatus).toHaveBeenCalledWith(1);
    expect(component.feedback[1].key).toBe('portal.command.offline');
    component.dismiss(1);
    expect(component.feedback[1]).toBeUndefined();
  });

  it('disables the remote actions of a charger that is not connected', () => {
    component.chargePoints = [{ ...point, online: false }];
    fixture.detectChanges();
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
    const restart = buttons.find(b => b.textContent!.includes('portal.partner.actions.hardReset'))!;

    expect(restart.disabled).toBeTrue();
    expect(text()).toContain('portal.partner.notConnected');
  });
});
