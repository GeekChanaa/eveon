/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { OCPPLocalListItemService } from './ocpp-local-list-item.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: OcppLocalListItem', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [OCPPLocalListItemService]
    });
  });

  it('should ...', inject([OCPPLocalListItemService], (service: OCPPLocalListItemService) => {
    expect(service).toBeTruthy();
  }));
});
