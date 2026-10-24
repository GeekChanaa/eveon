import { HttpErrorResponse } from '@angular/common/http';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerProfile, PartnerSession, PartnerStation, PartnerStatistics } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { PartnerPrintHeaderComponent } from './partner-print-header.component';
import { PartnerSessionsComponent } from './partner-sessions.component';

const profile: PartnerProfile = { id: 1, name: 'Atlas Energy', email: null, phone: null, city: 'Rabat', address: null, logoUrl: 'PartnersLogos/a.png',
  stationCount: 1, chargePointCount: 2 };
const stationRow: PartnerStation = { id: 4, name: 'Atlas Agdal', address: '12 Avenue', city: 'Rabat', status: 'Available', chargePointCount: 2,
  connectorCount: 3, latitude: '33.9', longitude: '-6.8', access: 'Public', photoUrl: 'images/charging-stations/a.png' };
const session = (id: number, active: boolean): PartnerSession => ({ id, stationName: 'Atlas Agdal', chargePointName: 'CP-1', connector: 'EVSE 1',
  startDate: '2026-10-20T09:00:00', endDate: active ? null : '2026-10-20T09:30:00', chargedMinutes: 30, chargedKwhs: 10, total: active ? 0 : 25,
  stoppedReason: 'Local', active });

describe('Partner sessions and printed pages', () => {
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

  describe('PartnerPrintHeaderComponent', () => {
    beforeEach(() => configureVxTest([PartnerPrintHeaderComponent], { imports: [PortalUiModule], providers: [{ provide: PartnerPortalService, useValue: portal }] }));

    it('prints the partner\'s logo and name next to eveon\'s, loading the profile when needed', () => {
      const fixture = TestBed.createComponent(PartnerPrintHeaderComponent);
      fixture.componentInstance.title = 'Sessions';
      fixture.detectChanges();
      fixture.detectChanges();

      expect(portal.profile).toHaveBeenCalledTimes(1);
      const logo = (fixture.nativeElement as HTMLElement).querySelector('.vx-print-header__partner') as HTMLImageElement;
      expect(logo.src).toContain('PartnersLogos/a.png');
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('Atlas Energy');
    });
  });

  describe('PartnerSessionsComponent', () => {
    beforeEach(() => configureVxTest([PartnerSessionsComponent], {
      imports: [PortalUiModule],
      providers: [
        { provide: PartnerPortalService, useValue: portal },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ station: '4' }) } } }
      ]
    }));

    it('filters on the station of the link and offers to stop only the running sessions', () => {
      const fixture = TestBed.createComponent(PartnerSessionsComponent);
      fixture.detectChanges();
      const stops = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('tbody button'));

      expect(portal.sessions.calls.mostRecent().args[0].stationId).toBe(4);
      expect(stops.length).toBe(1);
      expect(fixture.componentInstance.totals.revenue).toBe(25);
    });

    it('stops a running session after the confirmation and reloads once the charger had time to end it', fakeAsync(() => {
      portal.stopSession.and.returnValue(of({ message: '', status: 'Accepted' }));
      const fixture = TestBed.createComponent(PartnerSessionsComponent);
      fixture.detectChanges();
      const page = fixture.componentInstance;

      page.askStop(session(1, true));
      fixture.detectChanges();
      expect(page.stopDialog!.isOpen).toBeTrue();
      (fixture.nativeElement.querySelector('[data-confirm]') as HTMLButtonElement).click();

      expect(portal.stopSession).toHaveBeenCalledWith(1);
      expect(page.feedback?.key).toBe('portal.command.sessionStop.done');
      tick(4000);
      expect(portal.sessions).toHaveBeenCalledTimes(2);
    }));

    it('reloads when the session had already ended', () => {
      portal.stopSession.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { status: 'NotRunning' } })));
      const fixture = TestBed.createComponent(PartnerSessionsComponent);
      fixture.detectChanges();

      fixture.componentInstance.stopping = session(1, true);
      fixture.componentInstance.confirmStop();

      expect(fixture.componentInstance.feedback?.key).toBe('portal.command.notRunning');
      expect(portal.sessions).toHaveBeenCalledTimes(2);
    });

    it('tells when the statement cannot be downloaded', () => {
      portal.statement.and.returnValue(throwError(() => new Error('down')));
      const fixture = TestBed.createComponent(PartnerSessionsComponent);
      fixture.detectChanges();

      fixture.componentInstance.export('pdf');

      expect(fixture.componentInstance.exportError).toBeTrue();
    });
  });

});
