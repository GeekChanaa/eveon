import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ChargingSession } from 'src/_models/charging-session';


@Injectable({
  providedIn: 'root'
})
export class ChargingSessionService extends AbstractService<ChargingSession> {

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargingSession/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingSession/";

  getChargePointChargingSessions(chargePointID : number,page : number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointChargingSessions/"+chargePointID);
  }

  getChargingSessionInformations(chargingSessionID : number){
    return this.http.get(this.baseUrl+"GetChargingSessionInformations/"+chargingSessionID);
  }

  getChargingSessionInvoice(chargingSessionID: number) {
    this.http.get(`${this.baseUrl}GetChargingSessionInvoice/${chargingSessionID}`, { responseType: 'blob' })
      .subscribe((data: Blob) => {
        const blob = new Blob([data], { type: 'application/pdf' });
        const url = window.URL.createObjectURL(blob);
  
        // Create a link element and simulate a click
        const link = document.createElement('a');
        link.href = url;
        link.download = `ChargingSessionInvoice_${chargingSessionID}.pdf`;
        link.click();
  
        // Clean up the URL object
        window.URL.revokeObjectURL(url);
      });
  }
  

}
