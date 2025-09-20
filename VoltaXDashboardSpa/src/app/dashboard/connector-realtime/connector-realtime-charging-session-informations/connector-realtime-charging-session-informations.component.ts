import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChargingSessionService } from 'src/_services/charging-session.service';

@Component({
  selector: 'app-connector-realtime-charging-session-informations',
  templateUrl: './connector-realtime-charging-session-informations.component.html',
  styleUrls: ['./connector-realtime-charging-session-informations.component.sass']
})
export class ConnectorRealtimeChargingSessionInformationsComponent implements OnInit {

  @Input() chargingSessionID : number = 0;

  chargingSession : any = {};

  vatOption : string = "With VAT";

  isLoading : boolean = false;
  isDownloading = false;
  constructor(
    private _route: ActivatedRoute,
    private _chargingSessionService : ChargingSessionService
  ) { }

  ngOnInit() {
    var chargePointID = this._route.snapshot.paramMap.get('chargePointID');
    var chargingSessionID = this._route.snapshot.paramMap.get('id');
    if (chargingSessionID != null) {
      this.chargingSessionID = parseInt(chargingSessionID);
      this.getChargingSession(this.chargingSessionID);
    }

    this._chargingSessionService.isDownloading$.subscribe((status) => {
      this.isDownloading = status;
    });
  }

  getChargingSession(id : number){
    this.isLoading = true;
    this._chargingSessionService.getChargingSessionInformations(id).subscribe((data) => {
      this.isLoading = false;
      this.chargingSession = data;
      console.log("charging session" , this.chargingSession)
    })
  }

  getInvoice() {
    this._chargingSessionService.getChargingSessionInvoice(this.chargingSessionID);
  }

  onToggleChange(option: string): void {
    this.vatOption = option;
  }

}
