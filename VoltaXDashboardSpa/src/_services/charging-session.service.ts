import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ChargingSession } from 'src/_models/charging-session';
import { BehaviorSubject, Observable, Subject } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class ChargingSessionService extends AbstractService<ChargingSession> {

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargingSession/");
  }

  private isDownloadingSubject = new BehaviorSubject<boolean>(false);
  isDownloading$ = this.isDownloadingSubject.asObservable(); // Expose as Observable

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingSession/";

  getAllChargingSessions(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargingSessions");
  }

  getChargePointChargingSessions(chargePointID : number,page : number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointChargingSessions/"+chargePointID);
  }

  getUserChargingSessions(userID : number,page : number, itemsPerPage?: number, itemParams?: any){
    console.log("this is the getUser CharginSessions");
    console.log(userID,page,itemsPerPage);
    return super.getAll(page,itemsPerPage,itemParams,"GetUserChargingSessions/"+userID);
  }

  getChargingSessionInformations(chargingSessionID : number){
    return this.http.get(this.baseUrl+"GetChargingSessionInformations/"+chargingSessionID);
  }

  getChargingSessionInvoice(chargingSessionID: number): void {
    this.isDownloadingSubject.next(true); // Set to true when the call starts

    this.http
      .get(`${this.baseUrl}GetChargingSessionInvoice/${chargingSessionID}`, { responseType: 'blob' })
      .subscribe(
        (data: Blob) => {
          const blob = new Blob([data], { type: 'application/pdf' });
          const url = window.URL.createObjectURL(blob);

          const link = document.createElement('a');
          link.href = url;
          link.download = `ChargingSessionInvoice_${chargingSessionID}.pdf`;
          link.click();

          window.URL.revokeObjectURL(url);
        },
        (error) => {
          console.error('Error downloading invoice:', error);
        },
        () => {
          this.isDownloadingSubject.next(false); // Set to false when the call completes
        }
      );
  }

  getChargePointNbrChargingSessions(chargePointID : number){
    return this.http.get<number>(this.baseUrl+"GetChargePointNbrChargingSessions/"+chargePointID);
  }

  getChargePointNbrChargingSessionsToday(chargePointID : number){
    return this.http.get<number>(this.baseUrl+"GetChargePointNbrChargingSessionsToday/"+chargePointID);
  }

  getChargePointNbrChargingSessionsLast30Days(chargePointID : number){
    return this.http.get<any[]>(this.baseUrl+"GetChargePointNbrChargingSessionsLast30Days/"+chargePointID);
  }
  

}
