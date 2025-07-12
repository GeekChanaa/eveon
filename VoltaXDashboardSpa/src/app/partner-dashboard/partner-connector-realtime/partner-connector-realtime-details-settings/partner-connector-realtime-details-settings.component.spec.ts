/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { PartnerConnectorRealtimeDetailsSettingsComponent } from './partner-connector-realtime-details-settings.component';

describe('PartnerConnectorRealtimeDetailsSettingsComponent', () => {
  let component: PartnerConnectorRealtimeDetailsSettingsComponent;
  let fixture: ComponentFixture<PartnerConnectorRealtimeDetailsSettingsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PartnerConnectorRealtimeDetailsSettingsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerConnectorRealtimeDetailsSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
