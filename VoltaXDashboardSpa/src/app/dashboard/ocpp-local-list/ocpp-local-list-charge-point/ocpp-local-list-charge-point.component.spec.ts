/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { OcppLocalListChargePointComponent } from './ocpp-local-list-charge-point.component';

describe('OcppLocalListChargePointComponent', () => {
  let component: OcppLocalListChargePointComponent;
  let fixture: ComponentFixture<OcppLocalListChargePointComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OcppLocalListChargePointComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OcppLocalListChargePointComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
