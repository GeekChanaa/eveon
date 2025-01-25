import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';

@Component({
  selector: 'app-documentation-component',
  templateUrl: './documentation-component.component.html',
  styleUrls: ['./documentation-component.component.sass']
})
export class DocumentationComponentComponent implements OnInit {

  isLoading : boolean = false;
  component : any = {}

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
      this.getComponentByName(name);
    });
  }

  getComponentByName(name : string){
    this.isLoading = true;
    this._ocppComponentService.getComponentByName(name).subscribe((data) => {
      this.isLoading = false;
      this.component = data;
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong please contact your system admin",4000);
    })
  }

}
