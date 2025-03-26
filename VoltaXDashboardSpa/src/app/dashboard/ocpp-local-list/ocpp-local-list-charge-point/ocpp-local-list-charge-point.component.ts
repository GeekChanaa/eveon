import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OCPPLocalListItemService } from 'src/_services/ocpp-local-list/ocpp-local-list-item.service';
import { OCPPLocalListVersionService } from 'src/_services/ocpp-local-list/ocpp-local-list-version.service';

@Component({
  selector: 'app-ocpp-local-list-charge-point',
  templateUrl: './ocpp-local-list-charge-point.component.html',
  styleUrls: ['./ocpp-local-list-charge-point.component.sass']
})
export class OcppLocalListChargePointComponent implements OnInit {

  chargePointID : number = 0;
  localListItems : any[] = [];
  localListVersion : any = {};
  isLoading : boolean = false;
  constructor(
    private _route : ActivatedRoute,
    private _ocppLocalListService : OCPPLocalListVersionService
  ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      this.chargePointID = parseInt(idParam);
      this.getChargePointLocalList(this.chargePointID)
    }
  }

  getChargePointLocalList(chargePointID : number){
    this.isLoading = true;
    this._ocppLocalListService.getOCPPLocalListVersionLocalList(chargePointID).subscribe((data) => {
      console.log("this is the data");
      console.log(data);
      this.localListVersion = data;
      this.isLoading = false;
    });
  }

  refreshLocalListVersion(){
    
  }

}
