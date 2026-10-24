/* tslint:disable:no-unused-variable */
import { of } from 'rxjs';
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { DisplayTableListComponent } from './display-table-list.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('DisplayTableListComponent', () => {
  let component: DisplayTableListComponent;
  let fixture: ComponentFixture<DisplayTableListComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ DisplayTableListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DisplayTableListComponent);
    component = fixture.componentInstance;
    component.getItemsObservable = () => of({ result: [], pagination: { currentPage: 1, itemsPerPage: 10, totalItems: 0, totalPages: 0 } } as any);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
