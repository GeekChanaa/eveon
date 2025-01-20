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
  }>();

  confirmationRequest$ = this.confirmationRequest.asObservable();

  requestConfirmation(title: string, message: string, onConfirm: (...args: any[]) => void): void {
    this.confirmationRequest.next({ title, message, onConfirm });
  }
}
