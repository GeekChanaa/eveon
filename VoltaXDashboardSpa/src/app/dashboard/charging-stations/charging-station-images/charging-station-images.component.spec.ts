/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ChargingStationImagesComponent } from './charging-station-images.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('ChargingStationImagesComponent', () => {
  let component: ChargingStationImagesComponent;
  let fixture: ComponentFixture<ChargingStationImagesComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ ChargingStationImagesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargingStationImagesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
