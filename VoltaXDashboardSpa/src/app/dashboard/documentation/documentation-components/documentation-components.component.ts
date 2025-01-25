import { Component, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';

@Component({
  selector: 'app-documentation-components',
  templateUrl: './documentation-components.component.html',
  styleUrls: ['./documentation-components.component.sass']
})
export class DocumentationComponentsComponent implements OnInit {

  components : any[] =[];
  isLoading : boolean = false;

  constructor(
    private _ocppComponentService : OcppComponentsService,
    private _modalService:  ActionModalService
  ) { }

  ngOnInit() {
    this.getComponents();
  }

  getComponents(){
    this.isLoading = true;
    this._ocppComponentService.getComponents().subscribe((data) => {
      this.isLoading = false;
      this.components = data;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong please contact your system admin",4000);
    })
  }



}
