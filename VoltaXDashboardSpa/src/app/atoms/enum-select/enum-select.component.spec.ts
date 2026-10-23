/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { EnumSelectComponent } from './enum-select.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('EnumSelectComponent', () => {
  let component: EnumSelectComponent<any>;
  let fixture: ComponentFixture<EnumSelectComponent<any>>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ EnumSelectComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EnumSelectComponent);
    component = fixture.componentInstance;
    component.enumType = { Available: 0, Offline: 1 };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
