import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AtomsModule } from '../../atoms/atoms.module';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { AuditLogEntry, AuditLogFilters, AuditLogService } from 'src/_services/audit-log.service';

interface ChangeRow { property: string; old: string; next: string; }

@Component({
  selector: 'app-audit-logs-page',
  standalone: true,
  imports: [CommonModule, FormsModule, AtomsModule],
  templateUrl: './audit-logs-page.component.html',
  styleUrls: ['./audit-logs-page.component.sass']
})
export class AuditLogsPageComponent implements OnInit {
  readonly ready = PageState.Success;
  readonly pageSize = 25;

  filters: AuditLogFilters = { user: '', entityType: '', entityId: '', action: '', from: '', to: '' };
  entityTypes: string[] = [];
  items: AuditLogEntry[] = [];
  page = 1;
  totalCount = 0;
  loading = false;
  error = '';
  expandedId: number | null = null;

  constructor(private auditLogs: AuditLogService) {}

  ngOnInit() {
    this.auditLogs.entityTypes().subscribe({ next: types => this.entityTypes = types, error: () => this.entityTypes = [] });
    this.load();
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  search() {
    this.page = 1;
    this.load();
  }

  reset() {
    this.filters = { user: '', entityType: '', entityId: '', action: '', from: '', to: '' };
    this.search();
  }

  goTo(page: number) {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.page = page;
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    const filters: AuditLogFilters = { ...this.filters };
    // Date inputs are local days; send the whole day range in UTC.
    if (filters.from) filters.from = new Date(filters.from + 'T00:00:00').toISOString();
    if (filters.to) filters.to = new Date(filters.to + 'T23:59:59').toISOString();
    this.auditLogs.list(this.page, this.pageSize, filters).subscribe({
      next: result => {
        this.items = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: () => {
        this.items = [];
        this.totalCount = 0;
        this.loading = false;
        this.error = 'Could not load the audit log.';
      }
    });
  }

  toggle(entry: AuditLogEntry) {
    this.expandedId = this.expandedId === entry.id ? null : entry.id;
  }

  changes(entry: AuditLogEntry): ChangeRow[] {
    if (!entry.changesJson) return [];
    try {
      const parsed = JSON.parse(entry.changesJson);
      if (parsed === null || typeof parsed !== 'object') return [{ property: 'details', old: '', next: String(parsed) }];
      return Object.entries(parsed).map(([property, value]: [string, any]) => {
        const isDiff = value !== null && typeof value === 'object' && ('old' in value || 'new' in value || 'changed' in value);
        return isDiff
          ? { property, old: this.text(value.old), next: 'changed' in value ? '(changed)' : this.text(value.new) }
          : { property, old: '', next: this.text(value) };
      });
    } catch {
      return [{ property: 'details', old: '', next: entry.changesJson }];
    }
  }

  private text(value: unknown): string {
    if (value === undefined || value === null) return '';
    return typeof value === 'object' ? JSON.stringify(value) : String(value);
  }
}
