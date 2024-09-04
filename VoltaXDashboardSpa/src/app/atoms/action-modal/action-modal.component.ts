import { Component, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';


@Component({
  selector: 'app-action-modal',
  templateUrl: './action-modal.component.html',
  styleUrls: ['./action-modal.component.sass']
})
export class ActionModalComponent implements OnInit {

  isVisible: boolean = false;
  title: string = 'Title';
  message: string = 'User Created Successfully';
  image: string = '';

  constructor() { }

  ngOnInit(): void {}

  statusImages : any = {
    "success" : "/assets/images/icons/success.svg",
    "error" : "/assets/images/icons/error.svg",
    "warning" : "/assets/images/icons/warning.svg"
  }

  show(title : string,message: string, status : ActionModalStatusEnum): void {
    if(status == ActionModalStatusEnum.Success) this.image = this.statusImages["success"];
    if(status == ActionModalStatusEnum.Error) this.image = this.statusImages["error"];
    if(status == ActionModalStatusEnum.Warning) this.image = this.statusImages["warning"];
    this.message = message;
    this.title = title;
    this.isVisible = true;
  }

  hide(): void {
      this.isVisible = false;
  }

}
