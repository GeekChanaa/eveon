/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { OcppLocalListComponent } from './ocpp-local-list.component';

describe('OcppLocalListComponent', () => {
  let component: OcppLocalListComponent;
  let fixture: ComponentFixture<OcppLocalListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OcppLocalListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OcppLocalListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
