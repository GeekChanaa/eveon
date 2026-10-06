import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConfirmService {
  private confirmationRequest = new Subject<{
    title: string;
    message: string;
    onConfirm: (...args: any[]) => void;
    confirmLabel?: string;
    tone?: 'default' | 'danger';
  }>();

  confirmationRequest$ = this.confirmationRequest.asObservable();

  requestConfirmation(
    title: string,
    message: string,
    onConfirm: (...args: any[]) => void,
    options: { confirmLabel?: string; tone?: 'default' | 'danger' } = {}
  ): void {
    this.confirmationRequest.next({ title, message, onConfirm, ...options });
  }
}
