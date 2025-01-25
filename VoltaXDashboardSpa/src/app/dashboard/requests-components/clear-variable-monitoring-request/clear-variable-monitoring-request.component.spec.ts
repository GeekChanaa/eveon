/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ClearVariableMonitoringRequestComponent } from './clear-variable-monitoring-request.component';

describe('ClearVariableMonitoringRequestComponent', () => {
  let component: ClearVariableMonitoringRequestComponent;
  let fixture: ComponentFixture<ClearVariableMonitoringRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ClearVariableMonitoringRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ClearVariableMonitoringRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
