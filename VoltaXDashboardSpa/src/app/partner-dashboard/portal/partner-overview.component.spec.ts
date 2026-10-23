import { HttpErrorResponse } from '@angular/common/http';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerProfile, PartnerSession, PartnerStation, PartnerStatistics } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { PartnerOverviewComponent } from './partner-overview.component';
import { PartnerSecurityComponent } from './partner-security.component';

const profile: PartnerProfile = { id: 1, name: 'Atlas Energy', email: null, phone: null, city: 'Rabat', address: null, logoUrl: 'PartnersLogos/a.png',
  stationCount: 1, chargePointCount: 2 };
const stationRow: PartnerStation = { id: 4, name: 'Atlas Agdal', address: '12 Avenue', city: 'Rabat', status: 'Available', chargePointCount: 2,
  connectorCount: 3, latitude: '33.9', longitude: '-6.8', access: 'Public', photoUrl: 'images/charging-stations/a.png' };
const session = (id: number, active: boolean): PartnerSession => ({ id, stationName: 'Atlas Agdal', chargePointName: 'CP-1', connector: 'EVSE 1',
  startDate: '2026-10-20T09:00:00', endDate: active ? null : '2026-10-20T09:30:00', chargedMinutes: 30, chargedKwhs: 10, total: active ? 0 : 25,
  stoppedReason: 'Local', active });

describe('Partner overview and security', () => {
  let portal: jasmine.SpyObj<PartnerPortalService>;
  let profile$: BehaviorSubject<PartnerProfile | null>;

  beforeEach(() => {
    profile$ = new BehaviorSubject<PartnerProfile | null>(null);
    portal = jasmine.createSpyObj<PartnerPortalService>('PartnerPortalService',
      ['profile', 'overview', 'stations', 'chargePoints', 'sessions', 'statistics', 'statement', 'stopSession'], { profile$ });
    portal.profile.and.callFake(() => { profile$.next(profile); return of(profile); });
    portal.stations.and.returnValue(of([stationRow]));
    portal.chargePoints.and.returnValue(of([]));
    portal.sessions.and.returnValue(of({ items: [session(1, true), session(2, false)], page: 1, pageSize: 25, total: 2 }));
  });

  describe('PartnerOverviewComponent', () => {
    const statistics: PartnerStatistics = {
      daily: Array.from({ length: 30 }, (_, i) => ({ period: `2026-10-${String(i + 1).padStart(2, '0')}`, sessions: 1, energy: 10, revenue: 20 })),
      monthly: [], currency: 'MAD'
    };

    beforeEach(() => configureVxTest([PartnerOverviewComponent], { imports: [PortalUiModule], providers: [{ provide: PartnerPortalService, useValue: portal }] }));

    it('adds the revenue of the last 30 days as a chart', fakeAsync(() => {
      portal.overview.and.returnValue(of({ stations: 1, chargePoints: 2, connectors: 3, connectorsByStatus: { Available: 2, Occupied: 1 }, activeSessions: 1,
        revenueToday: 20, revenueMonth: 400, revenueTotal: 900, energyMonth: 200, sessionsMonth: 20, currency: 'MAD' }));
      portal.statistics.and.returnValue(of(statistics));
      const fixture = TestBed.createComponent(PartnerOverviewComponent);
      fixture.detectChanges();
      tick();
      const page = fixture.componentInstance;

      expect(page.revenue30).toBe(600);
      expect(page.trend.series[0].data.length).toBe(30);
      expect(page.trend.chart.type).toBe('area');
      fixture.destroy();
    }));

    it('still shows the overview when the statistics fail', fakeAsync(() => {
      portal.overview.and.returnValue(of({ stations: 0, chargePoints: 0, connectors: 0, connectorsByStatus: {}, activeSessions: 0,
        revenueToday: 0, revenueMonth: 0, revenueTotal: 0, energyMonth: 0, sessionsMonth: 0, currency: 'MAD' }));
      portal.statistics.and.returnValue(throwError(() => new Error('down')));
      const fixture = TestBed.createComponent(PartnerOverviewComponent);
      fixture.detectChanges();
      tick();

      expect(fixture.componentInstance.overview).not.toBeNull();
      expect(fixture.componentInstance.trend).toBeNull();
      expect(fixture.componentInstance.error).toBeFalse();
      fixture.destroy();
    }));
  });

  describe('PartnerSecurityComponent', () => {
    beforeEach(() => configureVxTest([PartnerSecurityComponent]));

    it('puts the two-factor settings in the portal layout', () => {
      const fixture = TestBed.createComponent(PartnerSecurityComponent);
      fixture.detectChanges();

      expect((fixture.nativeElement as HTMLElement).querySelector('.vx-heading h1')?.textContent).toContain('nav.security');
      expect((fixture.nativeElement as HTMLElement).querySelector('app-two-factor-settings')).not.toBeNull();
    });
  });
});
