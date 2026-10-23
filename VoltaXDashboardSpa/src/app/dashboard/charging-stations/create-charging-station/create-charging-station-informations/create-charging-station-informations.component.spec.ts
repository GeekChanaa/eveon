/* tslint:disable:no-unused-variable */
import { FormControl, FormGroup } from '@angular/forms';
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargingStationInformationsComponent } from './create-charging-station-informations.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('CreateChargingStationInformationsComponent', () => {
  let component: CreateChargingStationInformationsComponent;
  let fixture: ComponentFixture<CreateChargingStationInformationsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ CreateChargingStationInformationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargingStationInformationsComponent);
    component = fixture.componentInstance;
    component.form = new FormGroup({ chargerQuantity: new FormControl(1) });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
