import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';

let nextId = 0;

/**
 * A modal confirmation (native &lt;dialog&gt;: focus is kept inside, Escape cancels, focus goes back to the button
 * that opened it). The parent opens it with open(); Confirm closes it and emits confirmed, Cancel / Escape emit
 * cancelled. Extra explanation can be projected as content.
 */
@Component({
  selector: 'vx-confirm-dialog',
  template: `
    <dialog #dialog class="vx-dialog" [class.vx-dialog--danger]="tone === 'danger'" [attr.aria-labelledby]="id + '-title'"
      [attr.aria-describedby]="id + '-text'" (cancel)="onEscape($event)" (close)="restoreFocus()">
      <div class="vx-dialog__body">
        <span class="vx-dialog__icon" aria-hidden="true">
          <svg><use [attr.href]="tone === 'danger' ? '#icon-warning' : '#icon-info'"></use></svg>
        </span>
        <div class="vx-dialog__content">
          <h2 [id]="id + '-title'">{{ title }}</h2>
          <div [id]="id + '-text'" class="vx-dialog__text">
            <p *ngIf="message">{{ message }}</p>
            <ng-content></ng-content>
          </div>
        </div>
      </div>
      <div class="vx-dialog__actions">
        <button type="button" class="vx-btn" (click)="cancel()">{{ cancelLabel || ('common.actions.cancel' | translate) }}</button>
        <button type="button" class="vx-btn" [ngClass]="tone === 'danger' ? 'vx-btn--danger-solid' : 'vx-btn--primary'" (click)="confirm()"
          data-confirm>{{ confirmLabel || ('common.actions.confirm' | translate) }}</button>
      </div>
    </dialog>`
})
export class VxConfirmDialogComponent {
  @Input() title = '';
  @Input() message: string | null = null;
  @Input() confirmLabel = '';
  @Input() cancelLabel = '';
  /** danger: red confirm button and warning icon, for actions that interrupt charging or delete something. */
  @Input() tone: 'primary' | 'danger' = 'primary';
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild('dialog', { static: true }) private dialog!: ElementRef<HTMLDialogElement>;

  readonly id = `vx-dialog-${++nextId}`;
  private opener: HTMLElement | null = null;

  get isOpen(): boolean { return this.dialog.nativeElement.open; }

  open(): void {
    const dialog = this.dialog.nativeElement;
    if (dialog.open) return;
    this.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (typeof dialog.showModal === 'function') dialog.showModal();
    else dialog.setAttribute('open', '');
    // The safe choice gets the focus: Enter never confirms by accident.
    (dialog.querySelector('.vx-dialog__actions .vx-btn') as HTMLElement | null)?.focus();
  }

  close(): void {
    const dialog = this.dialog.nativeElement;
    if (!dialog.open) return;
    if (typeof dialog.close === 'function') dialog.close();
    else { dialog.removeAttribute('open'); this.restoreFocus(); }
  }

  confirm(): void {
    this.close();
    this.confirmed.emit();
  }

  cancel(): void {
    this.close();
    this.cancelled.emit();
  }

  /** Escape: the browser would close the dialog itself; route it through cancel() so the parent knows. */
  onEscape(event: Event): void {
    event.preventDefault();
    this.cancel();
  }

  restoreFocus(): void {
    this.opener?.focus?.();
    this.opener = null;
  }
}
