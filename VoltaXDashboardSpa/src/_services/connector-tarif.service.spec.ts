/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ConnectorTarifService } from './connector-tarif.service';

describe('Service: ConnectorTarif', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ConnectorTarifService]
    });
  });

  it('should ...', inject([ConnectorTarifService], (service: ConnectorTarifService) => {
    expect(service).toBeTruthy();
  }));
});
