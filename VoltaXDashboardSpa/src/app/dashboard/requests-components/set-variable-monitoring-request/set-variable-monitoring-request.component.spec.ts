/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SetVariableMonitoringRequestComponent } from './set-variable-monitoring-request.component';

describe('SetVariableMonitoringRequestComponent', () => {
  let component: SetVariableMonitoringRequestComponent;
  let fixture: ComponentFixture<SetVariableMonitoringRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SetVariableMonitoringRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SetVariableMonitoringRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
