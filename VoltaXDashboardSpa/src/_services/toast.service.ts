import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from './action-modal.service';

/**
 * Translated toasts on top of the dashboard's action modal: pages pass translation keys, never text.
 * {@link apiError} shows the API's own message when it sent one (validation error, conflict...).
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  constructor(private modal: ActionModalService, private translate: TranslateService) {}

  success(messageKey: string, params?: object): void {
    this.show(ActionModalStatusEnum.Success, 'operator.toast.success', messageKey, params, 3000);
  }

  warning(messageKey: string, params?: object): void {
    this.show(ActionModalStatusEnum.Warning, 'operator.toast.warning', messageKey, params, 4000);
  }

  error(messageKey = 'common.state.error', params?: object): void {
    this.show(ActionModalStatusEnum.Error, 'operator.toast.error', messageKey, params, 5000);
  }

  /** An HTTP failure: the API's message when it gave one, otherwise <paramref name="fallbackKey"/>. */
  apiError(error: unknown, fallbackKey = 'common.state.error'): void {
    const message = ToastService.apiMessage(error);
    if (message) this.modal.popup(ActionModalStatusEnum.Error, this.translate.instant('operator.toast.error'), message, 5000);
    else this.error(fallbackKey);
  }

  /** The human message of an API error body ({ error }, { message }, ProblemDetails title / errors, plain text), if any. */
  static apiMessage(error: unknown): string | null {
    if (!(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500) return null;
    const body = error.error;
    if (typeof body === 'string' && body.trim() && body.length < 300 && !body.trim().startsWith('<')) return body.trim();
    if (body && typeof body === 'object') {
      if (typeof body.error === 'string') return body.error;
      if (typeof body.message === 'string') return body.message;
      if (typeof body.Message === 'string') return body.Message;
      if (body.errors && typeof body.errors === 'object') {
        const first = Object.values(body.errors).flat().find(v => typeof v === 'string');
        if (typeof first === 'string') return first;
      }
      if (typeof body.title === 'string' && error.status !== 400) return body.title;
    }
    return null;
  }

  private show(status: ActionModalStatusEnum, titleKey: string, messageKey: string, params: object | undefined, duration: number): void {
    this.modal.popup(status, this.translate.instant(titleKey), this.translate.instant(messageKey, params), duration);
  }
}
