/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SetNetworkProfileRequestComponent } from './set-network-profile-request.component';

describe('SetNetworkProfileRequestComponent', () => {
  let component: SetNetworkProfileRequestComponent;
  let fixture: ComponentFixture<SetNetworkProfileRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SetNetworkProfileRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SetNetworkProfileRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
