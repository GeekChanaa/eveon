import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';


@Component({
  selector: 'app-action-modal',
  templateUrl: './action-modal.component.html',
  styleUrls: ['./action-modal.component.sass'],
  host: { class: 'vx-action-modal' }
})
export class ActionModalComponent implements OnInit, OnDestroy {

  isVisible: boolean = false;
  isMounted: boolean = false;
  isLeaving: boolean = false;
  title: string = 'Title';
  message: string = 'User Created Successfully';
  image: string = '';
  status: ActionModalStatusEnum = ActionModalStatusEnum.Success;
  private exitTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(private modalService: ActionModalService) { }

  ngOnInit(): void { this.modalService.setModal(this); }

  statusImages : any = {
    "success" : "/assets/images/icons/success.svg",
    "error" : "/assets/images/icons/error.svg",
    "warning" : "/assets/images/icons/warning.svg"
  }

  show(title : string,message: string, status : ActionModalStatusEnum): void {
    if (this.exitTimer) clearTimeout(this.exitTimer);
    if(status == ActionModalStatusEnum.Success) this.image = this.statusImages["success"];
    if(status == ActionModalStatusEnum.Error) this.image = this.statusImages["error"];
    if(status == ActionModalStatusEnum.Warning) this.image = this.statusImages["warning"];
    this.message = message;
    this.title = title;
    this.status = status;
    this.isMounted = true;
    this.isLeaving = false;
    // Wait a frame so the browser can animate from the initial position.
    requestAnimationFrame(() => this.isVisible = true);
  }

  hide(): void {
    if (!this.isMounted || this.isLeaving) return;
    this.isVisible = false;
    this.isLeaving = true;
    this.exitTimer = setTimeout(() => {
      this.isMounted = false;
      this.isLeaving = false;
      this.exitTimer = undefined;
    }, 220);
  }

  get statusClass(): string {
    return this.status === ActionModalStatusEnum.Error ? 'is-error'
      : this.status === ActionModalStatusEnum.Warning ? 'is-warning' : 'is-success';
  }

  ngOnDestroy(): void { if (this.exitTimer) clearTimeout(this.exitTimer); }

}
