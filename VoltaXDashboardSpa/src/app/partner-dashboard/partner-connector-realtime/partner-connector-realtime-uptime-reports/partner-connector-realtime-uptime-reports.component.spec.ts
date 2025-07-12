/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerConnectorRealtimeUptimeReportsComponent } from './partner-connector-realtime-uptime-reports.component';

describe('PartnerConnectorRealtimeUptimeReportsComponent', () => {
  let component: PartnerConnectorRealtimeUptimeReportsComponent;
  let fixture: ComponentFixture<PartnerConnectorRealtimeUptimeReportsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerConnectorRealtimeUptimeReportsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerConnectorRealtimeUptimeReportsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
