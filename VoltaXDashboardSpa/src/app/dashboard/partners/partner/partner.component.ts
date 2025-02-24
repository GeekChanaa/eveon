import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { PartnerService } from 'src/_services/partner.service';
import { environment } from 'src/environments/environment';

enum PartnerTabsEnum {
  InformationsTab = "InformationsTab",
  ChargingStationsTab = "ChargingStationsTab"
}

@Component({
  selector: 'app-partner',
  templateUrl: './partner.component.html',
  styleUrls: ['./partner.component.sass']
})
export class PartnerComponent implements OnInit {

  tabsEnum : PartnerTabsEnum = PartnerTabsEnum.InformationsTab;
  
    partnerID : number = 0;
    partnerLoaded : boolean = false;
    partnerTypeValues : { [key: number]: string; } = {};
    updatePartnerObservable = (id : number, model : any) => this._partnerService.edit(id, model);
  
    partner: any = {};
  
    staticUrl : string = environment.apiStaticFilesUrl;
  
    // Form group
    partnerForm : FormGroup;
  
    constructor(
      private _partnerService : PartnerService,
      private _route: ActivatedRoute,
      private _enumService : EnumMappingService
    ) {
      this.partnerForm = new FormGroup({
        serialNumber : new FormControl(''),
        make : new FormControl(''),
        status : new FormControl(''),
        category : new FormControl(''),
        comment : new FormControl(''),
        partnerCategory : new FormControl(''),
      })
     }
  
    ngOnInit() {
      var idParam = this._route.snapshot.paramMap.get('id')
      if (idParam != null) {
        var id = parseInt(idParam);
        this.getPartnerByID(id);
      }
    }
  
  
    getPartnerByID(id : number){
      this.partnerID = id;
      this._partnerService.getPartnerByID(id).subscribe((cs) => {
        this.partner = cs;
        this.partnerLoaded = true;
      })
    }
  
    changeTab(tab : any){
      this.tabsEnum = tab;
    }

}
