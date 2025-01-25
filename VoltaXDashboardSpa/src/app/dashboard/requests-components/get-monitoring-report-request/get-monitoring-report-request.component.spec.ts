/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { GetMonitoringReportRequestComponent } from './get-monitoring-report-request.component';

describe('GetMonitoringReportRequestComponent', () => {
  let component: GetMonitoringReportRequestComponent;
  let fixture: ComponentFixture<GetMonitoringReportRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GetMonitoringReportRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GetMonitoringReportRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
