import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AtomsModule } from '../../atoms/atoms.module';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { OcpiOutboxMessage, OcpiPartner, OcpiService, OcpiSettings, OcpiTokenA } from 'src/_services/ocpi.service';

@Component({
  selector: 'app-ocpi-page',
  standalone: true,
  imports: [CommonModule, FormsModule, AtomsModule],
  templateUrl: './ocpi-page.component.html',
  styleUrls: ['./ocpi-page.component.sass']
})
export class OcpiPageComponent implements OnInit {
  readonly ready = PageState.Success;
  readonly pageSize = 20;

  settings: OcpiSettings | null = null;
  partners: OcpiPartner[] = [];
  loading = false;
  error = '';
  message = '';
  busyId: number | null = null;
  expandedId: number | null = null;

  inviteName = '';
  invite: OcpiTokenA | null = null;
  connect = { name: '', versionsUrl: '', tokenA: '' };
  connecting = false;

  outbox: OcpiOutboxMessage[] = [];
  outboxPage = 1;
  outboxTotal = 0;

  constructor(private ocpi: OcpiService) {}

  ngOnInit() {
    this.ocpi.settings().subscribe({ next: s => this.settings = s, error: () => this.settings = null });
    this.load();
  }

  get outboxPages(): number { return Math.max(1, Math.ceil(this.outboxTotal / this.pageSize)); }

  load() {
    this.loading = true;
    this.error = '';
    this.ocpi.partners().subscribe({
      next: partners => { this.partners = partners; this.loading = false; },
      error: () => { this.partners = []; this.loading = false; this.error = 'Could not load the roaming partners.'; }
    });
    this.loadOutbox();
  }

  loadOutbox() {
    this.ocpi.outbox(this.outboxPage, this.pageSize).subscribe({
      next: result => { this.outbox = result.items; this.outboxTotal = result.totalCount; },
      error: () => { this.outbox = []; this.outboxTotal = 0; }
    });
  }

  goToOutbox(page: number) {
    if (page < 1 || page > this.outboxPages || page === this.outboxPage) return;
    this.outboxPage = page;
    this.loadOutbox();
  }

  createInvite() {
    if (!this.inviteName.trim()) return;
    this.message = '';
    this.ocpi.createTokenA(this.inviteName.trim()).subscribe({
      next: result => { this.invite = result; this.inviteName = ''; this.load(); },
      error: err => this.error = this.text(err, 'Could not generate a token.')
    });
  }

  copy(value: string) {
    navigator.clipboard?.writeText(value);
  }

  register() {
    const { name, versionsUrl, tokenA } = this.connect;
    if (!name.trim() || !versionsUrl.trim() || !tokenA.trim()) return;
    this.connecting = true;
    this.error = '';
    this.message = '';
    this.ocpi.register(name.trim(), versionsUrl.trim(), tokenA.trim()).subscribe({
      next: result => {
        this.connecting = false;
        this.connect = { name: '', versionsUrl: '', tokenA: '' };
        this.message = `Registered with ${result.countryCode}-${result.partyId}.`;
        this.load();
      },
      error: err => { this.connecting = false; this.error = this.text(err, 'Registration failed.'); this.load(); }
    });
  }

  suspend(partner: OcpiPartner) { this.run(partner, this.ocpi.suspend(partner.id), 'Partner suspended.'); }

  resume(partner: OcpiPartner) { this.run(partner, this.ocpi.resume(partner.id), 'Partner resumed.'); }

  refresh(partner: OcpiPartner) { this.run(partner, this.ocpi.refreshEndpoints(partner.id), 'Endpoints refreshed.'); }

  unregister(partner: OcpiPartner) {
    if (!confirm(`Unregister ${partner.name}? Both credentials tokens stop working immediately.`)) return;
    this.run(partner, this.ocpi.unregister(partner.id), 'Partner unregistered.');
  }

  retry(message: OcpiOutboxMessage) {
    this.ocpi.retry(message.id).subscribe({ next: () => this.load(), error: err => this.error = this.text(err, 'Retry failed.') });
  }

  toggle(partner: OcpiPartner) {
    this.expandedId = this.expandedId === partner.id ? null : partner.id;
  }

  private run(partner: OcpiPartner, request: ReturnType<OcpiService['suspend']>, done: string) {
    this.busyId = partner.id;
    this.error = '';
    this.message = '';
    request.subscribe({
      next: () => { this.busyId = null; this.message = done; this.load(); },
      error: err => { this.busyId = null; this.error = this.text(err, 'The action failed.'); }
    });
  }

  private text(err: HttpErrorResponse, fallback: string): string {
    return typeof err?.error === 'string' && err.error.length < 300 ? err.error : fallback;
  }
}
