import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';

@Component({
  selector: 'app-documentation-variable',
  templateUrl: './documentation-variable.component.html',
  styleUrls: ['./documentation-variable.component.sass']
})
export class DocumentationVariableComponent implements OnInit {

  isLoading : boolean = false;
  variable : any = {}

  constructor(
    private _ocppComponentService : OcppComponentsService,
    private _modalService : ActionModalService,
    private _route : ActivatedRoute
  ) { }

  ngOnInit() {
    this._route.paramMap.subscribe((params) => {
      let name = params.get('name');
      if(name == null){
        this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong please contact your system admin",4000);
        return;
      }
      this.getVariableByName(name);
    });
  }

  getVariableByName(name : string){
    this.isLoading = true;
    this._ocppComponentService.getVariableByName(name).subscribe((data) => {
      this.isLoading = false;
      this.variable = data;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong please contact your system admin",4000);
    })
  }

}
