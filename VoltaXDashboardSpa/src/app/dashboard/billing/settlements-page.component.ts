import { Component, OnInit } from '@angular/core';
import { saveDownload } from 'src/_helpers/download';
import { PagedResult } from 'src/_models/portal';
import { PartnerCommission, Settlement, SettlementDetail, SettlementFilter } from 'src/_models/billing';
import { AccessService } from 'src/_services/access.service';
import { BillingService } from 'src/_services/billing/billing.service';
import { LanguageService } from 'src/_services/language.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

const EMPTY: SettlementFilter = { partnerId: null, year: null, month: null, status: '', page: 1, pageSize: 25 };

/** A partner's commission terms being edited (percent of net revenue, MAD excl. VAT per kWh). */
interface TermsRow extends PartnerCommission { percent: number; perKwh: number; saving: boolean; saved: boolean; }

/**
 * Partner settlements for operators: every partner's monthly statements, generation on demand, payout recording
 * (date and reference), exports, and the commission terms of each partner.
 */
@Component({
  selector: 'app-settlements-page',
  standalone: true,
  imports: [PortalUiModule],
  templateUrl: './settlements-page.component.html'
})
export class SettlementsPageComponent implements OnInit {
  filter: SettlementFilter = { ...EMPTY };
  result: PagedResult<Settlement> | null = null;
  detail: SettlementDetail | null = null;
  terms: TermsRow[] = [];
  loading = false;
  error: string | null = null;
  message: string | null = null;
  exporting = false;

  /** Month to generate (defaults to last month). */
  generateYear: number;
  generateMonth: number;
  generatePartnerId: number | null = null;
  generating = false;

  /** The settlement whose payout is being recorded. */
  paying: Settlement | null = null;
  payReference = '';
  payDate = '';

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);
  readonly years = Array.from({ length: 4 }, (_, i) => new Date().getFullYear() - i);

  constructor(private billing: BillingService, private access: AccessService, private language: LanguageService) {
    const last = new Date();
    last.setDate(1);
    last.setMonth(last.getMonth() - 1);
    this.generateYear = last.getFullYear();
    this.generateMonth = last.getMonth() + 1;
  }

  get canEdit(): boolean { return this.access.can('EditPartners'); }

  ngOnInit(): void {
    this.load();
    this.loadTerms();
  }

  load(page = 1): void {
    this.filter.page = page;
    this.loading = true;
    this.billing.settlements(this.filter).subscribe({
      next: result => { this.result = result; this.loading = false; },
      error: response => { this.loading = false; this.fail(response); }
    });
  }

  reset(): void {
    this.filter = { ...EMPTY };
    this.load();
  }

  loadTerms(): void {
    this.billing.commissions().subscribe({
      next: rows => this.terms = rows.map(r => ({ ...r, percent: r.commissionPercent, perKwh: r.commissionPerKwh, saving: false, saved: false })),
      error: () => this.terms = []
    });
  }

  saveTerms(row: TermsRow): void {
    const percent = Number(row.percent), perKwh = Number(row.perKwh);
    if (!(percent >= 0 && percent <= 100) || !(perKwh >= 0 && perKwh <= 100)) {
      this.error = this.language.t('billing.settlements.termsRange');
      return;
    }
    row.saving = true;
    this.billing.updateCommission(row.partnerId, percent, perKwh).subscribe({
      next: saved => { Object.assign(row, saved, { percent: saved.commissionPercent, perKwh: saved.commissionPerKwh, saving: false, saved: true }); },
      error: response => { row.saving = false; this.fail(response); }
    });
  }

  generate(): void {
    this.generating = true;
    this.error = null;
    this.message = null;
    this.billing.generate(this.generateYear, this.generateMonth, this.generatePartnerId).subscribe({
      next: created => {
        this.generating = false;
        this.message = this.language.t('billing.settlements.generated', { count: created.length });
        this.filter = { ...EMPTY, year: this.generateYear, month: this.generateMonth };
        this.load();
      },
      error: response => { this.generating = false; this.fail(response); }
    });
  }

  open(settlement: Settlement): void {
    this.billing.settlement(settlement.id).subscribe({
      next: detail => this.detail = detail,
      error: response => this.fail(response)
    });
  }

  startPayment(settlement: Settlement): void {
    this.paying = settlement;
    this.payReference = '';
    this.payDate = new Date().toISOString().slice(0, 10);
  }

  confirmPayment(): void {
    if (!this.paying || this.payReference.trim().length === 0) {
      this.error = this.language.t('billing.settlements.referenceRequired');
      return;
    }
    const id = this.paying.id;
    this.billing.markPaid(id, this.payReference.trim(), this.payDate ? new Date(this.payDate + 'T12:00:00').toISOString() : null).subscribe({
      next: () => {
        this.paying = null;
        this.message = this.language.t('billing.settlements.paidSaved');
        this.load(this.filter.page);
        if (this.detail?.settlement.id === id) this.open(this.detail.settlement);
      },
      error: response => this.fail(response)
    });
  }

  export(settlement: Settlement, format: 'csv' | 'pdf'): void {
    this.exporting = true;
    this.billing.exportSettlement(settlement.id, format, this.language.current.code).subscribe({
      next: response => { saveDownload(response, `eveon-statement-${settlement.year}-${settlement.month}.${format}`); this.exporting = false; },
      error: response => { this.exporting = false; this.fail(response); }
    });
  }

  period(s: Settlement): Date { return new Date(s.year, s.month - 1, 1); }

  commission(s: Settlement): number { return s.commissionExclVat + s.commissionVat; }

  get pageTotals(): { gross: number; commission: number; payout: number } {
    const items = this.result?.items ?? [];
    return {
      gross: items.reduce((sum, s) => sum + s.grossAmount, 0),
      commission: items.reduce((sum, s) => sum + this.commission(s), 0),
      payout: items.reduce((sum, s) => sum + s.netPayout, 0)
    };
  }

  print(): void { window.print(); }

  private fail(response: any): void {
    this.error = response?.error?.error || response?.error?.title || this.language.t('common.state.error');
  }
}
