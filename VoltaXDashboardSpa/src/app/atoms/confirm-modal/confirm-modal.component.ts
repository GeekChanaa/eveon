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
  private onConfirmCallback!: (...args: any[]) => void;

  constructor(
    private _confirmService: ConfirmService
  ) {}

  ngOnInit(): void {
    this._confirmService.confirmationRequest$.subscribe(({ title, message, onConfirm }) => {
      this.title = title;
      this.message = message;
      this.onConfirmCallback = onConfirm;
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

}
