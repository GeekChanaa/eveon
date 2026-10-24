import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { saveDownload } from 'src/_helpers/download';
import { PagedResult, PartnerProfile } from 'src/_models/portal';
import { Settlement, SettlementDetail, SettlementLine } from 'src/_models/billing';
import { PartnerStatementsService } from 'src/_services/billing/billing.service';
import { LanguageService } from 'src/_services/language.service';
import { PartnerPortalService } from 'src/_services/portal/partner-portal.service';
import { PortalUiModule } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * The partner's monthly statements: what EVEON collected on its stations, the commission and VAT on it, the net
 * payout and its status. A statement opens with every session; CSV / PDF export and print.
 */
@Component({
  selector: 'app-partner-statements',
  standalone: true,
  imports: [PortalUiModule],
  templateUrl: './partner-statements.component.html'
})
export class PartnerStatementsComponent implements OnInit {
  private readonly statements = inject(PartnerStatementsService);
  private readonly portal = inject(PartnerPortalService);
  private readonly language = inject(LanguageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  result: PagedResult<Settlement> | null = null;
  detail: SettlementDetail | null = null;
  profile: PartnerProfile | null = null;
  year: number | null = null;
  readonly years: number[] = Array.from({ length: 4 }, (_, i) => new Date().getFullYear() - i);
  loading = false;
  error = false;
  exporting: string | null = null;
  readonly fileUrl = PartnerPortalService.fileUrl;

  ngOnInit(): void {
    this.portal.profile$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(profile => this.profile = profile);
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const id = Number(params.get('id'));
      if (Number.isInteger(id) && id > 0) this.open(id);
      else { this.detail = null; this.load(); }
    });
  }

  load(page = 1): void {
    this.loading = true;
    this.statements.list(this.year, page).subscribe({
      next: result => { this.result = result; this.loading = false; this.error = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  open(id: number): void {
    this.loading = true;
    this.statements.detail(id).subscribe({
      next: detail => { this.detail = detail; this.loading = false; this.error = false; },
      error: () => { this.detail = null; this.loading = false; this.error = true; }
    });
  }

  back(): void { this.router.navigate(['/partner-dashboard/statements']); }

  /** First day of the statement's month, for the month name pipe. */
  period(s: Settlement): Date { return new Date(s.year, s.month - 1, 1); }

  commission(s: Settlement): number { return s.commissionExclVat + s.commissionVat; }

  isRefund(line: SettlementLine): boolean { return line.refundId != null; }

  /** Totals of the lines shown, to print under the table (equal to the statement totals). */
  get lineTotals(): { kwh: number; gross: number; commission: number; payout: number } {
    const lines = this.detail?.lines ?? [];
    return {
      kwh: lines.reduce((sum, l) => sum + l.kwh, 0),
      gross: lines.reduce((sum, l) => sum + l.gross, 0),
      commission: lines.reduce((sum, l) => sum + l.commissionExclVat + l.commissionVat, 0),
      payout: lines.reduce((sum, l) => sum + l.netPayout, 0)
    };
  }

  export(s: Settlement, format: 'csv' | 'pdf'): void {
    this.exporting = `${s.id}-${format}`;
    this.statements.export(s.id, format, this.language.current.code).subscribe({
      next: response => { saveDownload(response, `eveon-statement-${s.year}-${s.month}.${format}`); this.exporting = null; },
      error: () => { this.exporting = null; this.error = true; }
    });
  }

  print(): void { window.print(); }
}
