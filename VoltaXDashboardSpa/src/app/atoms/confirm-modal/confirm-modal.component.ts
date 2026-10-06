import { Component, OnInit } from '@angular/core';
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
  confirmLabel = 'Confirm';
  tone: 'default' | 'danger' = 'default';
  private onConfirmCallback!: (...args: any[]) => void;

  constructor(
    private _confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
    this._confirmService.confirmationRequest$.subscribe(({ title, message, onConfirm, confirmLabel, tone }) => {
      this.title = title;
      this.message = message;
      this.onConfirmCallback = onConfirm;
      this.confirmLabel = confirmLabel || 'Confirm';
      this.tone = tone || 'default';
      this.isVisible = true;
    });
  }

  confirm() {
    if (this.onConfirmCallback) {
      this.onConfirmCallback();
    }
    this.isVisible = false;
  }

  cancel() {
    this.isVisible = false;
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.cancel();
  }

}
