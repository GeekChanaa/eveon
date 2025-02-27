import { Component, Input, OnInit } from '@angular/core';
import { ChargingStationService } from 'src/_services/charging-station.service';

@Component({
  selector: 'app-partner-charging-stations',
  templateUrl: './partner-charging-stations.component.html',
  styleUrls: ['./partner-charging-stations.component.sass']
})
export class PartnerChargingStationsComponent implements OnInit {

  @Input() partnerID : number = 0;
  chargingStations : any[] = [];
  cpfShow : Boolean = false;
  isChargingStationVisible : boolean = false;
  displayedChargingStationID : number = 0;

  constructor(
    private _chargingStationService: ChargingStationService
  ) { 
    
  }

  ngOnInit() {
    this.getPartnerChargingStations();
  }

  
  getPartnerChargingStations(){
    this._chargingStationService.getPartnerChargingStationsList(this.partnerID).subscribe((data) => {
      console.log("this is the partner data");
      console.log(data);
      this.chargingStations = data;
      this.cpfShow = false;
    })
  }
  

  refresh(){
    this.getPartnerChargingStations();
    this.isChargingStationVisible = false;  
  }

  showChargingStation(id : number){
    this.isChargingStationVisible = true;
    this.displayedChargingStationID = id;
  }

}
