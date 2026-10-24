/* tslint:disable:no-unused-variable */
import { FormControl, FormGroup } from '@angular/forms';
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SelectFormFieldComponent } from './select-form-field.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('SelectFormFieldComponent', () => {
  let component: SelectFormFieldComponent;
  let fixture: ComponentFixture<SelectFormFieldComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ SelectFormFieldComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectFormFieldComponent);
    component = fixture.componentInstance;
    component.parentForm = new FormGroup({ status: new FormControl('') });
    component.controlName = 'status';
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
