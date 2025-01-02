/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ConnectorRealtimeChargingSessionInformationsComponent } from './connector-realtime-charging-session-informations.component';

describe('ConnectorRealtimeChargingSessionInformationsComponent', () => {
  let component: ConnectorRealtimeChargingSessionInformationsComponent;
  let fixture: ComponentFixture<ConnectorRealtimeChargingSessionInformationsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ConnectorRealtimeChargingSessionInformationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConnectorRealtimeChargingSessionInformationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
