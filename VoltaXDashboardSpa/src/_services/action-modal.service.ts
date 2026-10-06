import { Injectable } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalComponent } from 'src/app/atoms/action-modal/action-modal.component';

@Injectable({
  providedIn: 'root'
})
export class ActionModalService {
  private modal: ActionModalComponent | undefined;
  private dismissTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {}

  setModal(modal: ActionModalComponent): void {
      this.modal = modal;
  }

  popup(status: ActionModalStatusEnum,title:string, message: string, duration: number,callback?: () => void): void {
    if (this.dismissTimer) clearTimeout(this.dismissTimer);
    this.modal?.show(title,message,status);
    this.dismissTimer = setTimeout(() => {
        this.modal?.hide();
        if(callback) callback();
        this.dismissTimer = undefined;
    }, duration);
  }

}
