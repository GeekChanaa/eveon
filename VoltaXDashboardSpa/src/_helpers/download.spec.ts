import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { saveDownload, toParams } from './download';

describe('download helpers', () => {
  it('builds query parameters from the filled filters only', () => {
    const params = toParams({ from: '2026-10-01', to: null, status: '', search: undefined, page: 0, stationId: 5 });

    expect(params.keys().sort()).toEqual(['from', 'page', 'stationId']);
    expect(params.get('page')).toBe('0');
  });

  it('saves the file under the name the server gave', () => {
    const clicked: string[] = [];
    spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) { clicked.push(this.download); });
    const headers = new HttpHeaders({ 'content-disposition': 'attachment; filename="eveon-sessions-20261023.csv"' });

    saveDownload(new HttpResponse({ body: new Blob(['a']), headers }), 'fallback.csv');
    saveDownload(new HttpResponse({ body: new Blob(['a']) }), 'fallback.csv');

    expect(clicked).toEqual(['eveon-sessions-20261023.csv', 'fallback.csv']);
  });
});
