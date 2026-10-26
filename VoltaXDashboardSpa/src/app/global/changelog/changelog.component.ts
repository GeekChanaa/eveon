import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

export type ReleaseComponent = 'dashboard' | 'api';

export interface Release {
  component: ReleaseComponent;
  version: string;
  date: string;
  changes: string[];
}

/** Written by .github/workflows/bump-version.yml (tools/release/bump-version.mjs), newest release first. */
export const CHANGELOG_URL = '/assets/data/changelog.json';

/** Public list of the dashboard and API releases with their changes. */
@Component({
  selector: 'app-changelog',
  templateUrl: './changelog.component.html',
  styleUrls: ['../legal-page.sass', './changelog.component.sass']
})
export class ChangelogComponent implements OnInit {
  readonly labels: Record<ReleaseComponent, string> = { dashboard: 'Dashboard', api: 'API' };
  readonly filters: { value: ReleaseComponent | 'all'; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'dashboard', label: 'Dashboard' },
    { value: 'api', label: 'API' }
  ];

  filter: ReleaseComponent | 'all' = 'all';
  releases: Release[] = [];
  loading = true;
  failed = false;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<{ releases: Release[] }>(CHANGELOG_URL).subscribe({
      next: (changelog) => {
        this.releases = changelog.releases ?? [];
        this.loading = false;
      },
      error: () => {
        this.failed = true;
        this.loading = false;
      }
    });
  }

  get visible(): Release[] {
    return this.filter === 'all' ? this.releases : this.releases.filter((r) => r.component === this.filter);
  }

  /** The current version of each component (the first, newest, entry). */
  get current(): Release[] {
    return (Object.keys(this.labels) as ReleaseComponent[])
      .map((component) => this.releases.find((r) => r.component === component))
      .filter((r): r is Release => !!r);
  }
}
