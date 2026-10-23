import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ChargingSessionService } from 'src/_services/charging-session.service';
import { ChargingSessionDetailComponent } from './charging-session-detail.component';

describe('ChargingSessionDetailComponent', () => {
  let service: jasmine.SpyObj<ChargingSessionService>;

  async function open(params: Record<string, string>, session: any) {
    service = jasmine.createSpyObj<ChargingSessionService>('ChargingSessionService', ['getChargingSessionInformations', 'getChargingSessionInvoice'],
      { isDownloading$: of(false) });
    service.getChargingSessionInformations.and.returnValue(session instanceof Error ? throwError(() => session) : of(session));
    await configureVxTest([ChargingSessionDetailComponent], {
      providers: [
        { provide: ChargingSessionService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(params) } } }
      ]
    });
    const fixture = TestBed.createComponent(ChargingSessionDetailComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  const billed = {
    id: 8, chargePointID: 3, chargePointName: 'CP-3', isBilled: true, kwhCharged: 12, chargedMinutes: 30, idleMinutes: 15,
    pricePerKWh: 2.5, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, energyPriceWithVAT: 30, timePriceWithVAT: 3, flatFeeWithVAT: 1,
    idldePriceWithVAT: 5, totalPriceWithVAT: 39, totalPriceWithoutVAT: 32.5, vatRate: 0.2, endDate: '2026-10-20T10:00:00Z'
  };

  it('itemizes a billed session: energy, time, session fee, idle time and VAT', async () => {
    const page = await open({ id: '8' }, billed);

    expect(page.state).toBe(PageState.Success);
    expect(page.lines.map(l => [l.label, l.amount])).toEqual([['Energy', 30], ['Charging time', 3], ['Session fee', 1], ['Idle time', 5]]);
    expect(page.vat).toBeCloseTo(6.5, 6);
    expect(page.vatPercent).toBe(20);
    expect(page.active).toBeFalse();
  });

  it('falls back to the time-based amount for sessions billed before amounts were stored', async () => {
    const page = await open({ id: '9' }, { id: 9, isBilled: false, chargedMinutes: 60, pricePerMinute: 0.5, chargingPriceWithVAT: 30, totalPriceWithVAT: 30, endDate: '2026-01-01' });

    expect(page.lines.map(l => [l.label, l.amount])).toEqual([['Charging time', 30]]);
  });

  it('breadcrumbs follow where the page was opened from', async () => {
    const fromList = await open({ id: '8' }, billed);
    expect(fromList.breadcrumbs[1]).toEqual({ label: 'Charging sessions', link: '/dashboard/charging-sessions' });
    expect(fromList.chargePointID).toBe(3);

    TestBed.resetTestingModule();
    const fromChargePoint = await open({ id: '8', chargePointID: '3' }, billed);
    expect(fromChargePoint.breadcrumbs[2]).toEqual({ label: 'Charge point #3', link: '/dashboard/connector-realtime/3' });
  });

  it('shows "not found" for an unknown session', async () => {
    const page = await open({ id: '404' }, Object.assign(new Error('missing'), { status: 404 }));
    expect(page.state).toBe(PageState.NotFound);
  });
});
