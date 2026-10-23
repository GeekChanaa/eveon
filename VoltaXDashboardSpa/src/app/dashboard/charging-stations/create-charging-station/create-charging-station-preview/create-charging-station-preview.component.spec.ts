/* tslint:disable:no-unused-variable */
import { FormControl, FormGroup } from '@angular/forms';
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { CreateChargingStationPreviewComponent } from './create-charging-station-preview.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('CreateChargingStationPreviewComponent', () => {
  let component: CreateChargingStationPreviewComponent;
  let fixture: ComponentFixture<CreateChargingStationPreviewComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ CreateChargingStationPreviewComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateChargingStationPreviewComponent);
    component = fixture.componentInstance;
    component.chargingStationInformationsForm = new FormGroup(Object.fromEntries(['status', 'category', 'chargerQuantity', 'parking', 'parkingType', 'restaurants', 'sittingArea', 'washroom', 'wifi'].map(k => [k, new FormControl('')])));
    component.chargingStationAddressForm = new FormGroup({ address: new FormControl(''), city: new FormControl(''), zipCode: new FormControl('') });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
