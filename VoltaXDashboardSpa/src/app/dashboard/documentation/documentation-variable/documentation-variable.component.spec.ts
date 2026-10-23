/* tslint:disable:no-unused-variable */
import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { DocumentationVariableComponent } from './documentation-variable.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';
describe('DocumentationVariableComponent', () => {
  let component: DocumentationVariableComponent;
  let fixture: ComponentFixture<DocumentationVariableComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ DocumentationVariableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DocumentationVariableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
