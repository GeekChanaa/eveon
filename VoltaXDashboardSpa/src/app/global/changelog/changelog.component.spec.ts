import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { CHANGELOG_URL, ChangelogComponent, Release } from './changelog.component';

import { VX_TEST_SCHEMAS, VxTestingModule } from 'src/testing/vx-testing.module';

const RELEASES: Release[] = [
  { component: 'dashboard', version: '1.1.0', date: '2026-11-02', changes: ['Changelog page'] },
  { component: 'api', version: '1.0.1', date: '2026-10-30', changes: ['Faster session list', 'Fix invoice totals'] },
  { component: 'dashboard', version: '1.0.0', date: '2026-10-26', changes: ['First release'] }
];

describe('ChangelogComponent', () => {
  let fixture: ComponentFixture<ChangelogComponent>;
  let component: ChangelogComponent;
  let http: HttpTestingController;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [VxTestingModule],
      schemas: VX_TEST_SCHEMAS,
      declarations: [ChangelogComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ChangelogComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists every release with its changes, newest first', () => {
    http.expectOne(CHANGELOG_URL).flush({ releases: RELEASES });
    fixture.detectChanges();

    const versions = Array.from(fixture.nativeElement.querySelectorAll('.changelog__release h2'))
      .map((h: any) => h.textContent.trim());
    expect(versions).toEqual(['1.1.0', '1.0.1', '1.0.0']);
    expect(text()).toContain('Fix invoice totals');
  });

  it('shows the current version of each component', () => {
    http.expectOne(CHANGELOG_URL).flush({ releases: RELEASES });

    expect(component.current.map((r) => `${r.component} ${r.version}`)).toEqual(['dashboard 1.1.0', 'api 1.0.1']);
  });

  it('filters the releases by component', () => {
    http.expectOne(CHANGELOG_URL).flush({ releases: RELEASES });

    component.filter = 'api';

    expect(component.visible.map((r) => r.version)).toEqual(['1.0.1']);
  });

  it('says so when the changelog cannot be loaded', () => {
    http.expectOne(CHANGELOG_URL).flush('', { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(text()).toContain('could not be loaded');
  });
});
