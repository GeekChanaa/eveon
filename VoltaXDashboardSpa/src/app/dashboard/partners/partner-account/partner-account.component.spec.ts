import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { PartnerAccountComponent } from './partner-account.component';
import { AccessService } from 'src/_services/access.service';
import { environment } from 'src/environments/environment';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';

describe('PartnerAccountComponent', () => {
  let fixture: ComponentFixture<PartnerAccountComponent>;
  let component: PartnerAccountComponent;
  let http: HttpTestingController;
  const access = { isAdmin: true };

  beforeEach(waitForAsync(() => {
    access.isAdmin = true;
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [PartnerAccountComponent],
      providers: [{ provide: AccessService, useValue: access }]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PartnerAccountComponent);
    component = fixture.componentInstance;
    component.partnerId = 7;
    component.email = 'contact@acme.ma';
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('offers administrators to create the account on the partner email', () => {
    expect(text()).toContain('contact@acme.ma');
    expect(fixture.nativeElement.querySelector('app-button')).not.toBeNull();
  });

  it('is hidden from everyone else', () => {
    access.isAdmin = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.partner-account')).toBeNull();
  });

  it('creates the account and says where the password went', () => {
    component.create();
    const request = http.expectOne(environment.apiUrl + '/api/partner/CreatePartnerUser/7');
    expect(request.request.method).toBe('POST');
    request.flush({ userID: 12, email: 'contact@acme.ma', recipients: 2 });
    fixture.detectChanges();

    expect(text()).toContain('emailed to 2 administrators');
    expect(fixture.nativeElement.querySelector('app-button')).toBeNull();
  });

  it('keeps the button when the API refuses', () => {
    component.create();
    http.expectOne(environment.apiUrl + '/api/partner/CreatePartnerUser/7')
      .flush({ error: 'contact@acme.ma is already used by another account.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(component.created).toBeNull();
    expect(fixture.nativeElement.querySelector('app-button')).not.toBeNull();
  });
});
