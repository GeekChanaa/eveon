/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { AddChargePointComponent } from './add-charge-point.component';

describe('AddChargePointComponent', () => {
  let component: AddChargePointComponent;
  let fixture: ComponentFixture<AddChargePointComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AddChargePointComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AddChargePointComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
