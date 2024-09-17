import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ChargingStationImage } from 'src/_models/charging-station-image';

@Injectable({
  providedIn: 'root'
})
export class ChargingStationImageService extends AbstractService<ChargingStationImage> {

  constructor(
    protected http : HttpClient
    ) {
    super(http,environment.apiUrl+"/api/ChargingStationImage/");
  }

  baseUrl = environment.apiUrl+"/api/ChargingStationImage/";

  getChargingStationImages(id : number){
    return this._http.get<any[]>(this.baseUrl+"GetChargingStationImages/"+id);
  }

  uploadChargingStationImages(images : FormData, chargingStationID : number){
    return this._http.post<any>(this.baseUrl+"UploadChargingStationImages/"+chargingStationID, images);
  }
}
