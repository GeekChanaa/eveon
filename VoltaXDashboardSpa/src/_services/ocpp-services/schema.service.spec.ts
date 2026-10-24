/* tslint:disable:no-unused-variable */

import { TestBed, waitForAsync, inject } from '@angular/core/testing';
import { SchemaService } from './schema.service';

import { VxTestingModule } from 'src/testing/vx-testing.module';
describe('Service: Schema', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      providers: [SchemaService]
    });
  });

  it('should ...', inject([SchemaService], (service: SchemaService) => {
    expect(service).toBeTruthy();
  }));
});
