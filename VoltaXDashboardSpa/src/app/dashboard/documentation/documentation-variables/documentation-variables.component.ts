import { Component, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';

@Component({
  selector: 'app-documentation-variables',
  templateUrl: './documentation-variables.component.html',
  styleUrls: ['./documentation-variables.component.sass']
})
export class DocumentationVariablesComponent implements OnInit {

  variables : any[] =[];
  isLoading : boolean = false;

  constructor(
    private _ocppComponentService : OcppComponentsService,
    private _modalService:  ActionModalService
  ) { }

  ngOnInit() {
    this.getVariables();
  }

  getVariables(){
    this.isLoading = true;
    this._ocppComponentService.getVariables().subscribe((data) => {
      this.isLoading = false;
      this.variables = data;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong please contact your system admin",4000);
    })
  }


}
