import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { configureVxTest } from 'src/testing/vx-testing.module';
import { PartnerStationDetail, StationOpeningHours } from 'src/_models/portal';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';
import { OpeningHoursEditorComponent, hoursByDay, weekdayName } from './opening-hours';
import { PartnerStationDetailComponent } from './partner-station-detail.component';

const station = (extra: Partial<PartnerStationDetail> = {}): PartnerStationDetail => ({
  id: 4, name: 'Atlas Agdal', address: '12 Avenue Fal Ould Oumeir', city: 'Rabat', latitude: 33.99, longitude: -6.85, status: 'Available',
  access: 'Public', parkingType: 'PerpendicularParking', description: null,
  openingHours: { twentyFourSeven: false, regular: [{ weekday: 1, periodBegin: '08:00', periodEnd: '20:00' }, { weekday: 6, periodBegin: '09:00', periodEnd: '24:00' }] },
  amenities: { wifi: true, parking: false, restaurants: false, washroom: true, sittingArea: false },
  photos: [{ id: 30, url: 'images/charging-stations/a.png' }], chargePointCount: 1, connectorCount: 2, updatedAt: '2026-10-20T09:00:00', ...extra
});

const png = (name = 'a.png', size = 10) => new File([new Uint8Array(size)], name, { type: 'image/png' });
const fileList = (...files: File[]) => files as unknown as FileList;

describe('Partner station page', () => {
  let portal: jasmine.SpyObj<PartnerPortalService>;
  let fixture: ComponentFixture<PartnerStationDetailComponent>;
  let page: PartnerStationDetailComponent;

  beforeEach(async () => {
    portal = jasmine.createSpyObj<PartnerPortalService>('PartnerPortalService',
      ['station', 'chargePoints', 'updateStation', 'addStationPhotos', 'removeStationPhoto', 'profile'], { profile$: of(null) });
    portal.station.and.returnValue(of(station()));
    portal.chargePoints.and.returnValue(of([]));
    await configureVxTest([PartnerStationDetailComponent, OpeningHoursEditorComponent], {
      imports: [PortalUiModule],
      providers: [
        { provide: PartnerPortalService, useValue: portal },
        { provide: ActivatedRoute, useValue: { paramMap: new BehaviorSubject(convertToParamMap({ id: '4' })) } }
      ]
    });
    fixture = TestBed.createComponent(PartnerStationDetailComponent);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent!;

  it('shows what drivers see: hours per day, amenities, photos and the charge points of the station', () => {
    expect(portal.station).toHaveBeenCalledWith(4);
    expect(portal.chargePoints).toHaveBeenCalledWith(4);
    expect(page.days.length).toBe(7);
    expect(text()).toContain('portal.station.amenity.wifi');
    expect(text()).not.toContain('portal.station.amenity.parking');
    expect(text()).toContain('09:00–24:00');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('.vx-photo-grid img').length).toBe(1);
  });

  it('says so when the station is not the partner\'s', () => {
    portal.station.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    fixture = TestBed.createComponent(PartnerStationDetailComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.notFound).toBeTrue();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('portal.station.notFound');
  });

  it('edits and saves the public information with the opening hours', () => {
    portal.updateStation.and.callFake((id, body) => of(station({ name: body.name, description: body.description })));
    page.startEditing();
    fixture.detectChanges();

    page.form.patchValue({ name: '  Atlas Agdal Centre ', description: 'Level -1', parking: true });
    page.save();

    const [id, body] = portal.updateStation.calls.mostRecent().args;
    expect(id).toBe(4);
    expect(body.name).toBe('Atlas Agdal Centre');
    expect(body.amenities.parking).toBeTrue();
    expect(body.openingHours.regular).toEqual(station().openingHours.regular);
    expect(page.editing).toBeFalse();
    expect(page.saved).toBeTrue();
    expect(page.station?.description).toBe('Level -1');
  });

  it('refuses half coordinates and keeps the form open', () => {
    page.startEditing();
    page.form.patchValue({ latitude: 33.9, longitude: null });

    page.save();

    expect(page.form.errors?.['coordinates']).toBeTrue();
    expect(portal.updateStation).not.toHaveBeenCalled();
  });

  it('points at the name when another station has it', () => {
    portal.updateStation.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'NameTaken' } })));
    page.startEditing();
    page.save();
    fixture.detectChanges();

    expect(page.nameTaken).toBeTrue();
    expect(page.editing).toBeTrue();
    expect(text()).toContain('portal.station.nameTaken');
  });

  it('checks photos before uploading them', () => {
    page.addPhotos(fileList(new File(['<svg/>'], 'x.svg', { type: 'image/svg+xml' })));
    expect(page.photoError).toBe('portal.station.photos.badType');

    page.addPhotos(fileList(png('big.png', 6 * 1024 * 1024)));
    expect(page.photoError).toBe('portal.station.photos.tooBig');

    page.addPhotos(fileList(...Array.from({ length: 8 }, (_, i) => png(`p${i}.png`))));
    expect(page.photoError).toBe('portal.station.photos.tooManyAtOnce');

    page.station = station({ photos: Array.from({ length: 7 }, (_, i) => ({ id: i, url: null })) });
    page.addPhotos(fileList(png(), png()));
    expect(page.photoError).toBe('portal.station.photos.tooMany');
    expect(portal.addStationPhotos).not.toHaveBeenCalled();
  });

  it('uploads photos and removes one after the confirmation', () => {
    portal.addStationPhotos.and.returnValue(of(station({ photos: [{ id: 30, url: 'a' }, { id: 31, url: 'b' }] })));
    portal.removeStationPhoto.and.returnValue(of(undefined));

    page.addPhotos(fileList(png()));
    expect(portal.addStationPhotos).toHaveBeenCalledWith(4, jasmine.any(Array));
    expect(page.station!.photos.length).toBe(2);

    page.askRemovePhoto(page.station!.photos[0]);
    fixture.detectChanges();
    expect(page.removePhotoDialog!.isOpen).toBeTrue();
    (fixture.nativeElement.querySelector('vx-confirm-dialog [data-confirm]') as HTMLButtonElement).click();

    expect(portal.removeStationPhoto).toHaveBeenCalledWith(4, 30);
    expect(page.station!.photos.map(p => p.id)).toEqual([31]);
  });
});

describe('OpeningHoursEditorComponent', () => {
  let editor: OpeningHoursEditorComponent;
  let emitted: StationOpeningHours[];

  beforeEach(async () => {
    await configureVxTest([OpeningHoursEditorComponent]);
    const fixture = TestBed.createComponent(OpeningHoursEditorComponent);
    editor = fixture.componentInstance;
    emitted = [];
    editor.hoursChange.subscribe(h => emitted.push(h));
    editor.hours = { twentyFourSeven: false, regular: [{ weekday: 2, periodBegin: '08:00', periodEnd: '24:00' }] };
    editor.ngOnChanges();
    fixture.detectChanges();
  });

  it('shows midnight as 00:00 and stores it as 24:00', () => {
    expect(editor.days[1].periods).toEqual([{ from: '08:00', to: '00:00' }]);
    expect(editor.value.regular).toEqual([{ weekday: 2, periodBegin: '08:00', periodEnd: '24:00' }]);
  });

  it('copies Monday to every day and adds periods', () => {
    editor.toggle(editor.days[0], true);
    editor.copyToAll(editor.days[0]);

    expect(editor.value.regular.length).toBe(7);
    expect(emitted.length).toBe(2);

    editor.addPeriod(editor.days[0]);
    expect(editor.days[0].periods.length).toBe(2);
  });

  it('finds what is wrong before the API does', () => {
    editor.days[1].periods = [{ from: '18:00', to: '08:00' }];
    expect(editor.problem).toBe('portal.station.hours.endAfterStart');

    editor.days[1].periods = [{ from: '08:00', to: '12:00' }, { from: '11:00', to: '14:00' }];
    expect(editor.problem).toBe('portal.station.hours.overlap');

    editor.days.forEach(d => d.open = false);
    expect(editor.problem).toBe('portal.station.hours.noneOpen');

    editor.setAlwaysOpen(true);
    expect(editor.problem).toBeNull();
    expect(emitted[emitted.length - 1]).toEqual({ twentyFourSeven: true, regular: [] });
  });

  it('starts regular hours from a usual week', () => {
    editor.days.forEach(d => { d.open = false; d.periods = []; });
    editor.setAlwaysOpen(false);

    expect(editor.value.regular.map(p => p.weekday)).toEqual([1, 2, 3, 4, 5, 6]);
  });

  it('names weekdays from Monday in the current language', () => {
    expect(weekdayName(1, 'en-US')).toBe('Monday');
    expect(weekdayName(7, 'fr')).toBe('dimanche');
    expect(hoursByDay({ twentyFourSeven: false, regular: [{ weekday: 3, periodBegin: '08:00', periodEnd: '09:00' }] })[2].periods.length).toBe(1);
  });
});
