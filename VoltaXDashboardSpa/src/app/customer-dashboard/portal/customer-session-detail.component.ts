import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { switchMap } from 'rxjs';
import { saveDownload } from 'src/_helpers/download';
import { MySessionDetail } from 'src/_models/portal';
import { CustomerPortalService } from 'src/_services/portal/customer-portal.service';
import { statusTone } from 'src/app/shared/portal-ui/portal-ui.module';

/** One charging session: where, when, how much energy and what it cost, with the invoice. */
@Component({
  selector: 'app-customer-session-detail',
  templateUrl: './customer-session-detail.component.html'
})
export class CustomerSessionDetailComponent implements OnInit {
  private readonly portal = inject(CustomerPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  detail: MySessionDetail | null = null;
  notFound = false;
  downloading = false;
  readonly tone = statusTone;

  ngOnInit(): void {
    this.route.paramMap.pipe(
      switchMap(params => this.portal.session(Number(params.get('id')))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({ next: detail => this.detail = detail, error: () => this.notFound = true });
  }

  invoice(): void {
    if (!this.detail) return;
    this.downloading = true;
    this.portal.sessionInvoice(this.detail.session.id).subscribe({
      next: response => { saveDownload(response, `eveon-session-${this.detail!.session.id}.pdf`); this.downloading = false; },
      error: () => this.downloading = false
    });
  }

  print(): void { window.print(); }
}
