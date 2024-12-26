/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { RequestRefreshChargerComponentsComponent } from './request-refresh-charger-components.component';

describe('RequestRefreshChargerComponentsComponent', () => {
  let component: RequestRefreshChargerComponentsComponent;
  let fixture: ComponentFixture<RequestRefreshChargerComponentsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RequestRefreshChargerComponentsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RequestRefreshChargerComponentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
