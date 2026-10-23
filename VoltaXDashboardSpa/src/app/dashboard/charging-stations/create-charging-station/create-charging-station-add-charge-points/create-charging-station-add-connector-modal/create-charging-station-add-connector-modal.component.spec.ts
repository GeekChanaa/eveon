/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargingStationAddConnectorModalComponent } from './create-charging-station-add-connector-modal.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('CreateChargingStationAddConnectorModalComponent', () => {
  let component: CreateChargingStationAddConnectorModalComponent;
  let fixture: ComponentFixture<CreateChargingStationAddConnectorModalComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ CreateChargingStationAddConnectorModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargingStationAddConnectorModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
