import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from 'src/environments/environment';
import { SystemReportCommentService } from 'src/_services/system-report-comment.service';

interface ReportComment { id: number; content: string; userName: string; createdAt: string; images?: string[]; }

/** The operators' discussion on a system report: comments in order and a box to add one. */
@Component({
  selector: 'app-system-report-comments',
  template: `
    <section class="vx-comments">
      <p class="vx-empty" *ngIf="loading">{{ 'common.state.loading' | translate }}</p>
      <p class="vx-empty" *ngIf="!loading && !comments.length">{{ 'ops.reports.noComments' | translate }}</p>
      <ol class="vx-comment-list" *ngIf="comments.length">
        <li *ngFor="let c of comments">
          <div class="vx-comment__head"><strong>{{ c.userName }}</strong><span class="muted">{{ c.createdAt | vxDate:'medium' }}</span></div>
          <p class="vx-comment__body">{{ c.content }}</p>
          <div class="vx-comment__images" *ngIf="c.images?.length">
            <a *ngFor="let image of c.images" [href]="staticUrl + image" target="_blank" rel="noopener"><img [src]="staticUrl + image" alt=""></a>
          </div>
        </li>
      </ol>
      <form class="vx-comment-form" (ngSubmit)="save()">
        <label for="comment" class="vx-sr-only">{{ 'ops.reports.comment' | translate }}</label>
        <textarea id="comment" name="comment" rows="3" maxlength="2000" [(ngModel)]="draft" [placeholder]="'ops.reports.placeholder' | translate"></textarea>
        <div class="vx-alert vx-alert--error" *ngIf="error" role="alert">{{ 'common.state.error' | translate }}</div>
        <div class="vx-actions">
          <button type="submit" class="vx-btn vx-btn--primary" [disabled]="saving || !draft.trim()">{{ 'ops.reports.post' | translate }}</button>
        </div>
      </form>
    </section>`
})
export class SystemReportCommentsComponent implements OnInit {
  private readonly service = inject(SystemReportCommentService);
  private readonly destroyRef = inject(DestroyRef);

  @Input() systemReportID = 0;
  comments: ReportComment[] = [];
  draft = '';
  loading = true;
  saving = false;
  error = false;
  readonly staticUrl = environment.apiStaticFilesUrl;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.service.getSystemReportComments(this.systemReportID, 1, 100).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: data => { this.comments = (data.result ?? []) as unknown as ReportComment[]; this.loading = false; },
      error: () => { this.loading = false; this.error = true; }
    });
  }

  save(): void {
    const content = this.draft.trim();
    if (!content) return;
    this.saving = true;
    this.error = false;
    this.service.createComment(this.systemReportID, content).subscribe({
      next: () => { this.saving = false; this.draft = ''; this.load(); },
      error: () => { this.saving = false; this.error = true; }
    });
  }
}
