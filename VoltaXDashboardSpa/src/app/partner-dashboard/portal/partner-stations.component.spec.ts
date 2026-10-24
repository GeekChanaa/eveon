import { HttpErrorResponse } from '@angular/common/http';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerProfile, PartnerSession, PartnerStation, PartnerStatistics } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { PartnerStationsComponent } from './partner-stations.component';

const profile: PartnerProfile = { id: 1, name: 'Atlas Energy', email: null, phone: null, city: 'Rabat', address: null, logoUrl: 'PartnersLogos/a.png',
  stationCount: 1, chargePointCount: 2 };
const stationRow: PartnerStation = { id: 4, name: 'Atlas Agdal', address: '12 Avenue', city: 'Rabat', status: 'Available', chargePointCount: 2,
  connectorCount: 3, latitude: '33.9', longitude: '-6.8', access: 'Public', photoUrl: 'images/charging-stations/a.png' };
const session = (id: number, active: boolean): PartnerSession => ({ id, stationName: 'Atlas Agdal', chargePointName: 'CP-1', connector: 'EVSE 1',
  startDate: '2026-10-20T09:00:00', endDate: active ? null : '2026-10-20T09:30:00', chargedMinutes: 30, chargedKwhs: 10, total: active ? 0 : 25,
  stoppedReason: 'Local', active });

describe('Partner stations', () => {
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

  describe('PartnerStationsComponent', () => {
    beforeEach(() => configureVxTest([PartnerStationsComponent], { imports: [PortalUiModule], providers: [{ provide: PartnerPortalService, useValue: portal }] }));

    it('shows each station as a card with its photo, linking to its page', () => {
      const fixture = TestBed.createComponent(PartnerStationsComponent);
      fixture.detectChanges();
      const element: HTMLElement = fixture.nativeElement;

      expect(element.querySelectorAll('.vx-station-card').length).toBe(1);
      expect((element.querySelector('.vx-station-card__photo img') as HTMLImageElement).src).toContain('images/charging-stations/a.png');
      expect(element.querySelector('a[href="/partner-dashboard/stations/4"]')).not.toBeNull();
      expect(element.textContent).toContain('portal.station.access.Public');
    });
  });

});
