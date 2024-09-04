import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { ActionModalComponent } from '../atoms/action-modal/action-modal.component';
import { ActionModalService } from 'src/_services/action-modal.service';
declare var $: any;  // Declare $ to use jQuery
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, AfterViewInit {

  constructor(
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    
  }

  ngAfterViewInit(): void {
    if(this.modal)
    this._modalService.setModal(this.modal);
  }

  @ViewChild(ActionModalComponent) modal: ActionModalComponent | undefined;

}
