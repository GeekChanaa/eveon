import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { ElectricVehicleModel } from 'src/_models/electric-vehicle-model';


@Injectable({
  providedIn: 'root'
})
export class ElectricVehicleModelService extends AbstractService<ElectricVehicleModel>{
  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/ElectricVehicleModel/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/ElectricVehicleModel/";

  getAllElectricVehicleModelsForSelect(){
    return this._http.get<any[]>(this.baseUrl+"GetAllElectricVehicleModelsForSelect/")
  }

}
