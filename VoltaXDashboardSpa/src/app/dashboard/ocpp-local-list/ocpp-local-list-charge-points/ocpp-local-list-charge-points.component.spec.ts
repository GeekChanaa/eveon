/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { OcppLocalListChargePointsComponent } from './ocpp-local-list-charge-points.component';

describe('OcppLocalListChargePointsComponent', () => {
  let component: OcppLocalListChargePointsComponent;
  let fixture: ComponentFixture<OcppLocalListChargePointsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OcppLocalListChargePointsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OcppLocalListChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
