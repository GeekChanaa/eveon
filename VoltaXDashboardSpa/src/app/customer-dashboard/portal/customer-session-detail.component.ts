import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Subject, catchError, map, merge, of, switchMap } from 'rxjs';
import { saveDownload } from 'src/_helpers/download';
import { MyActiveSession, MySessionDetail } from 'src/_models/portal';
import { AccessService } from 'src/_services/access.service';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { fullName, statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/**
 * One charging session: where, when, how much energy and what it cost, with the invoice and a printable receipt.
 * While it runs, the live widget shows it and lets the customer stop it.
 */
@Component({
  selector: 'app-customer-session-detail',
  templateUrl: './customer-session-detail.component.html'
})
export class CustomerSessionDetailComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly access = inject(AccessService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  detail: MySessionDetail | null = null;
  /** The live state of this session when it is the one charging now. */
  live: MyActiveSession | null = null;
  notFound = false;
  downloading = false;
  invoiceError = false;
  readonly tone = statusTone;

  get holder(): string | null { return fullName(this.access.current); }

  ngOnInit(): void {
    const id$ = this.route.paramMap.pipe(map(params => Number(params.get('id'))));
    merge(id$, this.reload$.pipe(map(() => this.detail?.session.id ?? 0))).pipe(
      switchMap(id => this.portal.session(id).pipe(catchError(() => of(null)))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(detail => {
      this.detail = detail;
      this.notFound = detail === null;
      this.live = null;
      if (detail?.session.active) this.loadLive(detail.session.id);
    });
  }

  /** The session ended while on screen: show its final amounts. */
  onEnded(): void { this.reload$.next(); }

  invoice(): void {
    if (!this.detail) return;
    this.downloading = true;
    this.invoiceError = false;
    this.portal.sessionInvoice(this.detail.session.id).subscribe({
      next: response => { saveDownload(response, `eveon-session-${this.detail!.session.id}.pdf`); this.downloading = false; },
      error: () => { this.downloading = false; this.invoiceError = true; }
    });
  }

  print(): void { window.print(); }

  private loadLive(id: number): void {
    this.portal.activeSession().pipe(catchError(() => of(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe(active => this.live = active?.id === id ? active : null);
  }
}
