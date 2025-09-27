import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChargePointStatisticsSummaryDto } from 'src/_models/_dtos/statistics-dtos/charge-point-summary-statistics-dto';
import { environment } from 'src/environments/environment';
import { AbstractService } from '../abstract-service';
import { ChargingStation } from 'src/_models/charging-station';

@Injectable({
  providedIn: 'root'
})
export class PartnerChargingStationService extends AbstractService<ChargingStation> {

  baseUrl = environment.apiUrl + "/api/partner/chargingStation/";

  constructor(protected http: HttpClient) {
    super(http, environment.apiUrl + "/api/partner/chargingStation/");
  }

  getTop10ChargingStations(partnerID : number, page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetPartnerTop10ChargingStations/"+partnerID);
  }
}
