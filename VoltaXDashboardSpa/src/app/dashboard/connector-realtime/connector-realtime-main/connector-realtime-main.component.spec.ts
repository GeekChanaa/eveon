import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AccessService } from 'src/_services/access.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargerConnectionService } from 'src/_services/charger-connection.service';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { ChargerNavbarEnum, ConnectorRealtimeMainComponent } from './connector-realtime-main.component';

describe('ConnectorRealtimeMainComponent', () => {
  let fixture: ComponentFixture<ConnectorRealtimeMainComponent>;
  let component: ConnectorRealtimeMainComponent;
  let status: Subject<boolean | null>;
  let chargePoints: jasmine.SpyObj<ChargePointService>;
  let connection: jasmine.SpyObj<ChargerConnectionService>;

  beforeEach(async () => {
    status = new Subject<boolean | null>();
    chargePoints = jasmine.createSpyObj<ChargePointService>('ChargePointService', ['getChargePointByID']);
    chargePoints.getChargePointByID.and.returnValue(of({ id: 7, chargePointId: 'CP-7' }));
    connection = jasmine.createSpyObj<ChargerConnectionService>('ChargerConnectionService', ['status']);
    connection.status.and.returnValue(status);
    await configureVxTest([ConnectorRealtimeMainComponent], {
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '7' }) } } },
        { provide: ChargePointService, useValue: chargePoints },
        { provide: ChargerConnectionService, useValue: connection },
        { provide: AccessService, useValue: { can: () => true, canUrl: () => true, isAdmin: true } }
      ]
    });
    fixture = TestBed.createComponent(ConnectorRealtimeMainComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  const actionsButton = () => [...fixture.nativeElement.querySelectorAll('.charger__navbar-item')]
    .find((b: HTMLButtonElement) => b.textContent?.includes('Actions')) as HTMLButtonElement;

  it('watches the connection of the charge point it shows', () => {
    expect(chargePoints.getChargePointByID).toHaveBeenCalledWith(7);
    expect(connection.status).toHaveBeenCalledWith('CP-7');
    expect(component.status).toBe('inactive');
  });

  it('only allows the Actions tab while the charger is connected', () => {
    status.next(false);
    fixture.detectChanges();
    expect(component.status).toBe('disconnected');
    expect(actionsButton().disabled).toBeTrue();
    component.selectMenuItem(ChargerNavbarEnum.Actions);
    expect(component.selectedMenuItem).toBe(ChargerNavbarEnum.Overview);

    status.next(true);
    fixture.detectChanges();
    expect(component.status).toBe('available');
    expect(actionsButton().disabled).toBeFalse();
    component.selectMenuItem(ChargerNavbarEnum.Actions);
    expect(component.selectedMenuItem).toBe(ChargerNavbarEnum.Actions);
  });

  it('leaves the Actions tab when the charger disconnects', () => {
    status.next(true);
    component.selectMenuItem(ChargerNavbarEnum.Actions);
    status.next(false);
    expect(component.selectedMenuItem).toBe(ChargerNavbarEnum.Overview);
    expect(component.online).toBeFalse();
  });

  it('shows an unknown status as inactive and stops watching when the page closes', () => {
    status.next(null);
    expect(component.status).toBe('inactive');
    fixture.destroy();
    expect(status.observed).toBeFalse();
  });

  it('reports a charge point that cannot be loaded', () => {
    chargePoints.getChargePointByID.and.returnValue(throwError(() => new Error('404')));
    component.getChargePointByID(8);
    expect(component.loadError).toBeTrue();
    expect(component.loading).toBeFalse();
  });
});
