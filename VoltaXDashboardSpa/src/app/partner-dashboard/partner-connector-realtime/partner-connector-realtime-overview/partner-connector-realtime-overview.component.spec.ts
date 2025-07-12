/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerConnectorRealtimeOverviewComponent } from './partner-connector-realtime-overview.component';

describe('PartnerConnectorRealtimeOverviewComponent', () => {
  let component: PartnerConnectorRealtimeOverviewComponent;
  let fixture: ComponentFixture<PartnerConnectorRealtimeOverviewComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerConnectorRealtimeOverviewComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerConnectorRealtimeOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
