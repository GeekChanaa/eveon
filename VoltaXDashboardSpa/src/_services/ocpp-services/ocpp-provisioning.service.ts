import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { PendingProvisioningChargePoint, ProvisioningPlan, ProvisioningResult, ProvisioningVariable } from 'src/_models/ocpp-provisioning';

@Injectable({
  providedIn: 'root'
})
export class OcppProvisioningService {

  baseUrl = environment.apiUrl + "/ocpp/provisioning/";

  constructor(private _http: HttpClient) { }

  getPending() {
    return this._http.get<PendingProvisioningChargePoint[]>(this.baseUrl + "Pending");
  }

  /** Reads the charger's full device model; can take up to ~2 minutes on slow chargers. */
  discover(chargePointID: string) {
    return this._http.post<ProvisioningPlan>(this.baseUrl + "Discover/" + encodeURIComponent(chargePointID), {});
  }

  apply(chargePointID: string, method: 'Automatic' | 'Manual', variables: ProvisioningVariable[]) {
    return this._http.post<ProvisioningResult>(this.baseUrl + "Apply/" + encodeURIComponent(chargePointID), { method, variables });
  }

  skip(chargePointID: string) {
    return this._http.post<ProvisioningResult>(this.baseUrl + "Skip/" + encodeURIComponent(chargePointID), {});
  }
}
