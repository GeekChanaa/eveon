/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargingStationChargePointsComponent } from './charging-station-charge-points.component';

describe('ChargingStationChargePointsComponent', () => {
  let component: ChargingStationChargePointsComponent;
  let fixture: ComponentFixture<ChargingStationChargePointsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargingStationChargePointsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargingStationChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
