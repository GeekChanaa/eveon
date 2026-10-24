import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ConfirmService } from 'src/_services/confirm.service';

@Component({
  selector: 'app-confirm-modal',
  templateUrl: './confirm-modal.component.html',
  styleUrls: ['./confirm-modal.component.sass']
})
export class ConfirmModalComponent implements OnInit {
  isVisible = false;
  title = '';
  message = '';
  /** Empty: the translated "Confirm". */
  confirmLabel = '';
  tone: 'default' | 'danger' = 'default';
  private onConfirmCallback?: (...args: any[]) => void;
  private onCancelCallback?: () => void;
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private _confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
    this._confirmService.confirmationRequest$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(({ title, message, onConfirm, onCancel, confirmLabel, tone }) => {
      // A new question replaces one left open: that one counts as cancelled.
      if (this.isVisible) this.onCancelCallback?.();
      this.title = title;
      this.message = message;
      this.onConfirmCallback = onConfirm;
      this.onCancelCallback = onCancel;
      this.confirmLabel = confirmLabel || '';
      this.tone = tone || 'default';
      this.isVisible = true;
    });
  }

  confirm() {
    this.isVisible = false;
    this.onCancelCallback = undefined;
    this.onConfirmCallback?.();
  }

  cancel() {
    if (!this.isVisible) return;
    this.isVisible = false;
    const cancelled = this.onCancelCallback;
    this.onCancelCallback = undefined;
    cancelled?.();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.cancel();
  }

}
