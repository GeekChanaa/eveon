import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject } from 'rxjs';

export interface ConfirmationRequest {
  title: string;
  message: string;
  onConfirm: (...args: any[]) => void;
  onCancel?: () => void;
  confirmLabel?: string;
  tone?: 'default' | 'danger';
}

/** What {@link ConfirmService.ask} shows: translation keys (and their parameters), never text. */
export interface ConfirmKeys {
  title: string;
  message: string;
  params?: object;
  /** Default: common.actions.confirm (common.actions.delete for tone "danger"). */
  confirmLabel?: string;
  tone?: 'default' | 'danger';
}

/** The dashboard's confirmation dialog (app-confirm-modal, in the dashboard shell). */
@Injectable({
  providedIn: 'root'
})
export class ConfirmService {
  private confirmationRequest = new Subject<ConfirmationRequest>();

  confirmationRequest$ = this.confirmationRequest.asObservable();

  constructor(private translate: TranslateService) {}

  /** Shows the dialog with texts already in the user's language. */
  requestConfirmation(
    title: string,
    message: string,
    onConfirm: (...args: any[]) => void,
    options: { confirmLabel?: string; tone?: 'default' | 'danger'; onCancel?: () => void } = {}
  ): void {
    this.confirmationRequest.next({ title, message, onConfirm, ...options });
  }

  /**
   * Asks with translation keys; emits true and completes when confirmed, completes without value when cancelled.
   * Use for every destructive action: `confirm.ask({ ... }).subscribe(() => this.delete())`.
   */
  ask(keys: ConfirmKeys): Observable<true> {
    return new Observable<true>(subscriber => {
      const t = (key: string) => this.translate.instant(key, keys.params);
      this.requestConfirmation(t(keys.title), t(keys.message), () => { subscriber.next(true); subscriber.complete(); }, {
        tone: keys.tone,
        confirmLabel: t(keys.confirmLabel ?? (keys.tone === 'danger' ? 'common.actions.delete' : 'common.actions.confirm')),
        onCancel: () => subscriber.complete()
      });
    });
  }
}
