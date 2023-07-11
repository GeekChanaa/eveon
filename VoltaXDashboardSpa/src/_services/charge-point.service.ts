import { Injectable } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PaginatedResult } from 'src/_models/pagination';
@Injectable({
  providedIn: 'root'
})
export class ChargePointService extends AbstractService<ChargePoint>{

  constructor(
    protected http : HttpClient,
    protected snackBar : MatSnackBar
    ) {
    super(http,snackBar, environment.apiUrl+"/api/chargepoint/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargepoint/";


  override create(model: any): Observable<ChargePoint> {
    model.chargePointID = this.generateChargePointID();
    return this._http.post<ChargePoint>(this.actionUrl, model, this.httpOptions).pipe(map(response => {
      this.snackBar.open('Charge Point Created', 'Dismiss', {
        duration: 2000,
      });
      return response;
    }));
  }


  generateChargePointID(): string {
    const alphaNumChars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    const length = alphaNumChars.length;
    let result = 'VX'; // Start with 'VX'

    // Generate the 'VVVVVVVV' part
    for (let i = 0; i < 8; i++) {
      const randomIndex = Math.floor(Math.random() * length);
      result += alphaNumChars[randomIndex];
    }

    return result;
  }

  // Get Partner Charge Points : 
  getPartnerChargePoints(partnerID : number,page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<ChargePoint[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"GetPartnerChargePoints/"+partnerID);
  }
}
