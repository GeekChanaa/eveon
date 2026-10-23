import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { environment } from 'src/environments/environment';
import { VxTestingModule } from 'src/testing/vx-testing.module';
import { CustomerPortalService } from './customer-portal.service';
import { PartnerPortalService } from './partner-portal.service';
import { PaymentsService } from './payments.service';
import { OperatorSearchService } from '../operator-search.service';

const api = environment.apiUrl;

describe('Portal API services', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [VxTestingModule] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('CustomerPortalService', () => {
    it('sends only the filters that are set', () => {
      TestBed.inject(CustomerPortalService).sessions({ from: '2026-10-01', to: null, status: '', search: 'Marina', page: 2, pageSize: 20 }).subscribe();

      const request = http.expectOne(r => r.url === `${api}/api/my/sessions`);
      expect(request.request.params.keys().sort()).toEqual(['from', 'page', 'pageSize', 'search']);
      expect(request.request.params.get('search')).toBe('Marina');
      request.flush({ items: [], page: 2, pageSize: 20, total: 0 });
    });

    it('exports the filtered sessions without paging and as a file', () => {
      TestBed.inject(CustomerPortalService).exportSessions({ status: 'completed', page: 3, pageSize: 20 }, 'pdf').subscribe();

      const request = http.expectOne(r => r.url === `${api}/api/my/sessions/export`);
      expect(request.request.params.get('format')).toBe('pdf');
      expect(request.request.params.get('status')).toBe('completed');
      expect(request.request.params.has('page')).toBeFalse();
      expect(request.request.responseType).toBe('blob');
      request.flush(new Blob(['%PDF']));
    });

    it('is read only: every call is a GET', () => {
      const portal = TestBed.inject(CustomerPortalService);
      portal.overview().subscribe();
      portal.profile().subscribe();
      portal.cards().subscribe();
      portal.wallet().subscribe();
      portal.orders().subscribe();
      portal.session(4).subscribe();

      const requests = http.match(() => true);
      expect(requests.length).toBe(6);
      requests.forEach(r => { expect(r.request.method).toBe('GET'); r.flush({}); });
    });
  });

  describe('PaymentsService', () => {
    it('starts a top-up with the amount and card', () => {
      TestBed.inject(PaymentsService).startTopUp(150, 9).subscribe();

      const request = http.expectOne(`${api}/api/payments/top-ups`);
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({ amount: 150, cardId: 9 });
      request.flush({ orderId: 1, checkout: { method: 'GET', url: 'https://pay.test' } });
    });
  });

  describe('PartnerPortalService', () => {
    it('puts the four prices of one connector', () => {
      const pricing = { pricePerKWh: 3.5, pricePerMinute: 0.1, pricePerIdleMinute: 0.5, flatFee: 2 };
      TestBed.inject(PartnerPortalService).updatePricing(12, pricing).subscribe();

      const request = http.expectOne(`${api}/api/partner-portal/connectors/12/pricing`);
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual(pricing);
      request.flush({ id: 12, ...pricing });
    });

    it('shares the loaded profile (sidebar logo) and the uploaded logo', () => {
      const service = TestBed.inject(PartnerPortalService);
      const seen: (string | null | undefined)[] = [];
      service.profile$.subscribe(p => seen.push(p?.logoUrl));

      service.profile().subscribe();
      http.expectOne(`${api}/api/partner-portal/profile`).flush({ id: 1, name: 'Volt', logoUrl: '/files/a.png' });
      service.uploadLogo(new File(['x'], 'logo.png', { type: 'image/png' })).subscribe();
      const upload = http.expectOne(`${api}/api/partner-portal/profile/logo`);
      expect(upload.request.body instanceof FormData).toBeTrue();
      upload.flush({ id: 1, name: 'Volt', logoUrl: '/files/b.png' });

      expect(seen).toEqual([undefined, '/files/a.png', '/files/b.png']);
    });

    it('resolves stored file paths against the static files host', () => {
      expect(PartnerPortalService.fileUrl(null)).toBeNull();
      expect(PartnerPortalService.fileUrl('https://cdn.test/x.png')).toBe('https://cdn.test/x.png');
      expect(PartnerPortalService.fileUrl('/files/x.png')).toBe(environment.apiStaticFilesUrl + '/files/x.png');
    });
  });

  describe('OperatorSearchService', () => {
    it('searches sessions and exports transactions with the same filters', () => {
      const service = TestBed.inject(OperatorSearchService);
      service.sessions({ search: '#42', status: 'active', page: 1, pageSize: 25 }).subscribe();
      service.exportTransactions({ status: 'Ended', page: 4, pageSize: 25 }).subscribe();

      const sessions = http.expectOne(r => r.url === `${api}/api/ChargingSession/Search`);
      expect(sessions.request.params.get('search')).toBe('#42');
      sessions.flush({ items: [], page: 1, pageSize: 25, total: 0 });
      const exported = http.expectOne(r => r.url === `${api}/api/Transaction/Export`);
      expect(exported.request.params.get('status')).toBe('Ended');
      expect(exported.request.params.has('page')).toBeFalse();
      exported.flush(new Blob(['a;b']));
    });
  });
});
