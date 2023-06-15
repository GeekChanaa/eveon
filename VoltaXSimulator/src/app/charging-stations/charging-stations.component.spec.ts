/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargingStationsComponent } from './charging-stations.component';

describe('ChargingStationsComponent', () => {
  let component: ChargingStationsComponent;
  let fixture: ComponentFixture<ChargingStationsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargingStationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargingStationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
