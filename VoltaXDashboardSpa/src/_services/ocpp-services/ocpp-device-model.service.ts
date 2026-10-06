import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import {
  ChargePointConfigurationStatus, DeviceModelReportResult, DeviceModelSetResult, DeviceModelVariable, ReportBase
} from 'src/_models/charge-point-configuration';
import { ProvisioningVariable, ProvisioningVariableResult } from 'src/_models/ocpp-provisioning';

/** A charge point's OCPP 2.0.1 device model: GetBaseReport, GetVariables and SetVariables. */
@Injectable({
  providedIn: 'root'
})
export class OcppDeviceModelService {

  baseUrl = environment.apiUrl + "/ocpp/DeviceModel/";

  constructor(private _http: HttpClient) { }

  overview() {
    return this._http.get<ChargePointConfigurationStatus[]>(this.baseUrl + "Overview");
  }

  status(chargePointID: string) {
    return this._http.get<ChargePointConfigurationStatus>(this.baseUrl + "Status/" + encodeURIComponent(chargePointID));
  }

  variables(chargePointID: string) {
    return this._http.get<DeviceModelVariable[]>(this.baseUrl + "Variables/" + encodeURIComponent(chargePointID));
  }

  /** Waits for the whole NotifyReport; can take up to ~2 minutes on slow chargers. */
  report(chargePointID: string, reportBase: ReportBase) {
    return this._http.post<DeviceModelReportResult>(this.baseUrl + "Report/" + encodeURIComponent(chargePointID), { reportBase });
  }

  get(chargePointID: string, variables: ProvisioningVariable[]) {
    return this._http.post<ProvisioningVariableResult[]>(this.baseUrl + "Get/" + encodeURIComponent(chargePointID), { variables });
  }

  set(chargePointID: string, variables: ProvisioningVariable[]) {
    return this._http.post<DeviceModelSetResult>(this.baseUrl + "Set/" + encodeURIComponent(chargePointID), { variables });
  }

  reboot(chargePointID: string, immediate: boolean) {
    return this._http.post<{ status: string }>(this.baseUrl + "Reboot/" + encodeURIComponent(chargePointID), { immediate });
  }
}
