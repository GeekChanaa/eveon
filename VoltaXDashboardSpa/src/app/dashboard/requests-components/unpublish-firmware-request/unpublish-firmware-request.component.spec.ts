/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { UnpublishFirmwareRequestComponent } from './unpublish-firmware-request.component';

describe('UnpublishFirmwareRequestComponent', () => {
  let component: UnpublishFirmwareRequestComponent;
  let fixture: ComponentFixture<UnpublishFirmwareRequestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ UnpublishFirmwareRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UnpublishFirmwareRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
