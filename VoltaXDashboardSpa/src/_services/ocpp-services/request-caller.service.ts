import { Injectable } from '@angular/core';
import { OcppConfigurationService } from './ocpp-configuration.service';
import { OcppEvDriverService } from './ocpp-ev-driver.service';
import { OcppReportingService } from './ocpp-reporting.service';
import { OcppMonitoringService } from './ocpp-monitoring.service';
import { OcppSmartChargingService } from './ocpp-smart-charging.service';

@Injectable({
  providedIn: 'root'
})
export class RequestCallerService {

  constructor(
    private _configurationService : OcppConfigurationService,
    private _evDriverService : OcppEvDriverService,
    private _reportingService : OcppReportingService,
    private _smartChargingService : OcppSmartChargingService,
    private _monitoringService: OcppMonitoringService
  ) { }

  sendOCPPMessage(chargePointID: string,requestName: string, request: any){
    switch (requestName) {
      case "Authorize":

        break;
      case "BootNotification":

        break;
      case "CancelReservation":
        return this._evDriverService.cancelReservation(chargePointID,request);
        break;
      case "CertificateSigned":

        break;
      case "ChangeAvailability":
        return this._configurationService.changeAvailability(chargePointID,request);
        break;
      case "ClearCache":
        return this._evDriverService.clearCache(chargePointID,request);
        break;
      case "ClearChargingProfile":
        return this._smartChargingService.clearChargingProfile(chargePointID,request);
        break;
      case "ClearDisplayMessage":
        return this._configurationService.clearDisplayMessage(chargePointID,request);
        break;
      case "ClearedChargingLimit":
        // Sent by the charger, never by the CSMS.
        break;
      case "ClearVariableMonitoring":
        return this._monitoringService.clearVariableMonitoring(chargePointID,request);
        break;
      case "CostUpdated":

        break;
      case "CustomerInformation":
        return this._reportingService.customerInformation(chargePointID,request);
        break;
      case "DataTransfer":

        break;
      case "DeleteCertificate":

        break;
      case "FirmwareStatusNotification":

        break;
      case "Get15118EVCertificate":

        break;
      case "GetBaseReport":
        return this._reportingService.getBaseReport(chargePointID,request);
        break;
      case "GetCertificateStatus":

        break;
      case "GetChargingProfiles":
        return this._smartChargingService.getChargingProfiles(chargePointID,request);
        break;
      case "GetCompositeSchedule":
        return this._smartChargingService.getCompositeSchedule(chargePointID,request);
        break;
      case "GetDisplayMessages":
        return this._configurationService.getDisplayMessages(chargePointID,request);
        break;
      case "GetInstalledCertificateIds":

        break;
      case "GetLocalListVersion":
        return this._evDriverService.getLocalListVersion(chargePointID,request);
        break;
      case "GetLog":
        return this._reportingService.getLog(chargePointID,request);
        break;
      case "GetMonitoringReport":
        return this._reportingService.getMonitoringReport(chargePointID,request);
        break;
      case "GetReport":
        return this._reportingService.getReport(chargePointID,request);
        break;
      case "GetTransactionStatus":

        break;
      case "GetVariables":
        return this._monitoringService.getVariables(chargePointID,request);
        break;
      case "Heartbeat":

        break;
      case "InstallCertificate":

        break;
      case "LogStatusNotification":

        break;
      case "MeterValues":

        break;
      case "NotifyChargingLimit":

        break;
      case "NotifyCustomerInformation":

        break;
      case "NotifyDisplayMessages":

        break;
      case "NotifyEVChargingNeeds":

        break;
      case "NotifyEVChargingSchedule":

        break;
      case "NotifyEvent":

        break;
      case "NotifyMonitoringReport":

        break;
      case "NotifyReport":

        break;
      case "PublishFirmware":
        this._configurationService.publishFirmware(chargePointID,request);
        break;
      case "PublishFirmwareStatusNotification":

        break;
      case "ReportChargingProfiles":

        break;
      case "RequestStartTransaction":
        return this._evDriverService.requestStartTransaction(chargePointID,request);
        break;
      case "RequestStopTransaction":
        return this._evDriverService.requestStopTransaction(chargePointID,request);
        break;
      case "ReservationStatusUpdate":

        break;
      case "ReserveNow":
        return this._evDriverService.reserveNow(chargePointID,request);
        break;
      case "Reset":
        return this._configurationService.reset(chargePointID,request);
        break;
      case "SecurityEventNotification":

        break;
      case "SendLocalList":
        return this._evDriverService.sendLocalList(chargePointID,request);
        break;
      case "SetChargingProfile":
        return this._smartChargingService.setChargingProfile(chargePointID,request);
        break;
      case "SetDisplayMessage":
        return this._configurationService.setDisplayMessage(chargePointID,request);
        break;
      case "SetMonitoringBase":
        return this._monitoringService.setMonitoringBase(chargePointID,request);
        break;
      case "SetMonitoringLevel":
        return this._monitoringService.setMonitoringLevel(chargePointID,request);
        break;
      case "SetNetworkProfile":
        return this._configurationService.setNetworkProfile(chargePointID,request);
        break;
      case "SetVariableMonitoring":
        return this._monitoringService.setVariableMonitoring(chargePointID,request);
        break;
      case "SetVariables":
        return this._monitoringService.setVariables(chargePointID,request);
        break;
      case "SignCertificate":

        break;
      case "StatusNotification":

        break;
      case "TransactionEvent":

        break;
      case "TriggerMessage":
        return this._configurationService.triggerMessage(chargePointID,request);
        break;
      case "UnlockConnector":
        return this._evDriverService.unlockConnector(chargePointID,request);
        break;
      case "UnpublishFirmware":
        return this._configurationService.unpublishFirmware(chargePointID,request);
        break;
      case "UpdateFirmware":
        return this._configurationService.updateFirmware(chargePointID,request);
        break;
    
      default:
        break;
    }
    return  null;
  }


}
