/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargePointsComponent } from './charge-points.component';

describe('ChargePointsComponent', () => {
  let component: ChargePointsComponent;
  let fixture: ComponentFixture<ChargePointsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ChargePointsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargePointsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
