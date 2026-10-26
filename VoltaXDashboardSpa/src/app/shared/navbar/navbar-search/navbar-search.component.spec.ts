/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { NavbarSearchComponent } from './navbar-search.component';
import { AccessService } from 'src/_services/access.service';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('NavbarSearchComponent', () => {
  let component: NavbarSearchComponent;
  let fixture: ComponentFixture<NavbarSearchComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ NavbarSearchComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NavbarSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('lists a page once when it is also a recent search, with its real route', (done) => {
    spyOn(TestBed.inject(AccessService), 'canUrl').and.returnValue(true);
    localStorage.setItem('recentSearches', JSON.stringify([
      { type: 'recent', title: 'Charging Stations', route: '/dashboard/charging-stationsf' },
      { type: 'recent', title: 'Charging Stations', route: '/dashboard/charging-stations' }
    ]));
    component.toggleSearch();

    component.search('charging stations').subscribe(results => {
      expect(results.map(r => r.route)).toEqual(['/dashboard/charging-stations']);
      localStorage.removeItem('recentSearches');
      done();
    });
  });
});
