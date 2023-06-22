import { formatDate } from '@angular/common';
import { Injectable } from '@angular/core';
import { connect, map } from 'rxjs';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket';
import { TransactionEventRequest } from 'src/_models/TransactionEventRequest';
import { ConnectorStatusEnumType } from 'src/_models/_enums/ConnectorStatusEnumType';
class MyWebSocket extends WebSocket {
  constructor(url: string) {
    super(url, ["ocpp2.0", "ocpp1.5"]);
  }
}

@Injectable({
  providedIn: 'root'
})
export class WebsocketService {

  private myWebSocket!: WebSocketSubject<any>;
  private wsLink : string = "";
  private _id : string = "";
  private _connector_locked : boolean = false;
  private _lastTranId : string = "";

  constructor() {
  }

  setWsLink(wsLink : string){
    this.wsLink = wsLink;
    const websocketConstructor = (url: string) => {
      return new WebSocket(url, ["ocpp2.0", "ocpp1.5"]);
    };

    this.myWebSocket = webSocket({
      url: this.wsLink,
      WebSocketCtor: MyWebSocket,
    });
  }

  // Connecting the websocket
  wsConnect(){
    this.onMessageReceived().subscribe(
      (msg) => {
        console.log('Connection established:', msg);
      },
      (err) => {
        // This will be called if there's an error.
        console.log('Websocket Error:', err);
      },
      () => {
        // This will be called when the WebSocket is closed.
        console.log('Websocket Closed');
      }
    );
  }

  public onMessageReceived() {
    return this.myWebSocket.asObservable().pipe(
      map(msg => {
        let c = 0;
        console.log("this is the message data") ;
        console.log(msg);
        var ddata = msg;
        console.log(ddata);

        if (c === 1) {
          const hb_interval = this.handleData(ddata);
          sessionStorage.setItem("Configuration", hb_interval);
          this.startHB(hb_interval * 1000);
        }

        if (ddata[0] === 3) {
          var la = this.getLastAction();

          if (la == "startTransaction") {

            ddata = ddata[2];
            console.log("Data exchange successful!");
            var array = Object.values(ddata);
            var TransactionId: string = typeof array[0] === 'string' ? array[0] : '';
            sessionStorage.setItem('TransactionId', TransactionId);
          }
          console.log("Response recieved successfully!");
        } else if ((msg)[0] === 4) {
          console.log("Data exchange failed - JSON is not accepted!");
        } else if ((JSON.parse(msg))[0] === 2) {
          console.log((JSON.parse(msg))[2]);
          this._id = (JSON.parse(msg))[1];

          switch (ddata[2]) {
            case "ChangeAvailability":
              var changeAvail = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(changeAvail);
              break;
            case "SetVariables":
              var setVar = JSON.stringify([3, this._id, {
                "setVariableResult": [{
                  "attributeStatus": "Accepted",
                  "component": {
                    "name": "AuthCtrlr"
                  },
                  "variable": {
                    "name": "variable name"
                  }
                }]
              }]);
              this.myWebSocket.next(setVar);
              break;
            case "ClearCache":
              var clearCache = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(clearCache);
              break;
            case "GetLog":
              var getLog = JSON.stringify([3, this._id, {
                "status": "Accepted",
                "filename": "somefile.txt"
              }]);
              this.myWebSocket.next(getLog);
              break;
            // case "RequestStartTransaction":
            //   var reqStartTrans = JSON.stringify([3, this._id, {
            //     "status": "Accepted"
            //   }]);
            //   this.myWebSocket.next(reqStartTrans);
            //   this.startTransaction();
            //   break;
            case "RequestStopTransaction":
              var reqStopTrans = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(reqStopTrans);

              var stop_id = (JSON.parse(msg.data)[3].transactionId);
              //this.stopTransaction(stop_id,);
              break;
            case "Reset":
              var ResetS = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(ResetS);
              //location.reload();
              break;
            case "UnlockConnector":
              var unlockCon = JSON.stringify([3, this._id, {
                "status": "Unlocked"
              }]);
              this.myWebSocket.next(unlockCon);
              this._connector_locked = false;
              // $('.indicator').hide();
              // $('#yellow').show();
              console.log("Connector status changed to: " + this._connector_locked);
              // this.statusNotification();
              break;
            case "UpdateFirmware":
              var updFirm = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(updFirm);
              break;
            case "ReserveNow":
              var resNow = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(resNow);
              break;
            case "CancelReservation":
              var cancelRes = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(cancelRes);
              break;
            case "DataTransfer":
              var dataTrans = JSON.stringify([3, this._id, {
                "status": "Accepted",
                "data": "some data"
              }]);
              this.myWebSocket.next(dataTrans);
              break;
            case "GetVariables":
              var getVars = JSON.stringify([3, this._id, {
                "getVariableResult": [{
                  "attributeStatus": "Accepted",
                  "attributeValue": "true",
                  "variable": {
                    "name": "SampledDataAvailable"
                  },
                  "component": {
                    "name": "DeviceDataCtrlr"
                  }
                }, {
                  "attributeStatus": "Accepted",
                  "attributeValue": "360",
                  "variable": {
                    "name": "AlignedDataInterval"
                  },
                  "component": {
                    "name": "DeviceDataCtrlr"
                  }
                }, {
                  "attributeStatus": "UnknownVariable",
                  "attributeValue": "10",
                  "variable": {
                    "name": "RestartDelay"
                  },
                  "component": {
                    "name": "DeviceDataCtrlr"
                  }
                }]
              }]);
              this.myWebSocket.next(getVars);
              break;
            case "GetLocalListVersion":
              var gLocList = JSON.stringify([3, this._id, {
                "versionNumber": 0
              }]);
              this.myWebSocket.next(gLocList);
              break;
            case "SendLocalList":
              var sendLocList = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(sendLocList);
              break;
            case "TriggerMessage":
              var triggerMsg = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(triggerMsg);

              var req_message = (JSON.parse(msg.data)[3].requestedMessage);
              if (req_message == "BootNotification") {
                this.bootNotification();
              }
              if (req_message == "LogStatusNotification") {}
              if (req_message == "FirmwareStatusNotification") {
                this.firmwareStatusNotification();
              }
              if (req_message == "Heartbeat") {
                this.send_heartbeat();
              }
              if (req_message == "MeterValues") {
                this.meterValues();
              }
              if (req_message == "SignChargingStationCertificate") {}
              if (req_message == "SignV2GCertificate") {}
              // if (req_message == "StatusNotification") {
              //   this.statusNotification();
              // }
              if (req_message == "TransactionEvent") {}
              break;
            case "GetCompositeSchedule":
              var getComp = JSON.stringify([3, this._id, {
                "status": 'Accepted',
                "evseId": 0,
                "schedule": {
                  "startDateTime": this.formatDate(new Date()),
                  "chargingSchedule": {
                    "duration": 86400,
                    "chargingRateUnit": "W",
                    "chargingSchedulePeriod": [{
                      "startPeriod": 0,
                      "limit": 11000,
                      "numberPhases": 3
                    }, {
                      "startPeriod": 28800,
                      "limit": 6000,
                      "numberPhases": 3
                    }, {
                      "startPeriod": 72000,
                      "limit": 11000,
                      "numberPhases": 3
                    }]
                  }
                }
              }]);
              this.myWebSocket.next(getComp);
              break;
            case "ClearChargingProfile":
              var clearChargPrfl = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(clearChargPrfl);
              break;
            case "SetChargingProfile":
              var setChargPrlf = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(setChargPrlf);
              break;
            case "CertificateSigned":
              var certSigned = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(certSigned);
              break;
            case "ClearDisplayMessage":
              var clearDispMsg = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(clearDispMsg);
              break;
            case "ClearVariableMonitoring":
              var clearVarMon = JSON.stringify([3, this._id, {
                "clearMonitoringResult": [{
                  "id": 1,
                  "status": "Accepted"
                }, {
                  "id": 2,
                  "status": "Rejected"
                }, {
                  "id": 3,
                  "status": "NotFound"
                }]
              }]);
              this.myWebSocket.next(clearVarMon);
              break;
            case "GetBaseReport":
              var getBaseRep = JSON.stringify([3, this._id, {
                "status": "Accepted"
              }]);
              this.myWebSocket.next(getBaseRep);
              break;
            case "CostUpdated":
              var costUpdtd = JSON.stringify([3, this._id, {}]);
              this.myWebSocket.next(costUpdtd);
              break;
            case "GetMonitoringReport":
              var getMonRep = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(getMonRep);
              break;
            case "GetReport":
              var getRep = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(getRep);
              break;
            case "SetMonitoringBase":
              var setMonBase = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(setMonBase);
              break;
            case "SetMonitoringLevel":
              var setMonLevel = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(setMonLevel);
              break;
            case "SetVariableMonitoring":
              var setVarMon = JSON.stringify([3, this._id, {"setMonitoringResult": [{
                "id": 1,
                "type": "Periodic",
                "severity": 9,
                "status": "Accepted",
                "component": {
                  "name": "some component"
                  },
                "variable":{
                  "name": "some variable"
                  }
              }]}]);
              this.myWebSocket.next(setVarMon);
              break;
            case "UnpublishFirmware":
              var unpFirm = JSON.stringify([3, this._id, {"status": "Unpublished"}]);
              this.myWebSocket.next(unpFirm);
              break;
            case "CustomerInformation":
              var custmrInfo = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(custmrInfo);
              break;
            case "SetDisplayMessage":
              var setmessage = (JSON.parse(msg.data)[3].message.message.content);
              console.log("Display message: <b><i>" + setmessage + "</i></b>");
              var setDispMsg = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(setDispMsg);
              break;
            case "DeleteCertificate":
              var delCert = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(delCert);
              break;
            case "GetChargingProfiles":
              var getChargPrfl = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(getChargPrfl);
              break;
            case "GetDisplayMessages":
              var getDispMsg = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(getDispMsg);
              break;
            case "GetInstalledCertificateIds":
              var getInstalledCertIds = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(getInstalledCertIds);
              break;
            case "GetTransactionStatus":
              var getTranStat = JSON.stringify([3, this._id, {"messagesInQueue": "true"}]);
              this.myWebSocket.next(getTranStat);
              break;
            case "InstallCertificate":
              var installCert = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(installCert);
              break;
            case "NotifyCentralChargingNeeds":
              var noCentChargNeed = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(noCentChargNeed);
              break;
            case "PublishFirmware":
              var pubFirmware = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(pubFirmware);
              break;
            case "Renegotiate15118Schedule":
              var renSchedule = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(renSchedule);
              break;
            case "SetNetworkProfile":
              var setNetProfl = JSON.stringify([3, this._id, {"status": "Accepted"}]);
              this.myWebSocket.next(setNetProfl);
              break;
            default:
              var error = JSON.stringify([4, this._id]);
              this.myWebSocket.next(error);
              break;
          }
        }

        // The rest of your logic...
      })
    );
  }

  bootNotification() {

    var BN = JSON.stringify([2, this._id, "BootNotification", {
      "chargingStation": {
        "serialNumber": "avt.001.13.1",
        "model": "AVT-Express",
        "modem": {
          "iccid": "MMCC IINN NNNN NNNN NN C x",
          "imsi": "520031234567890"
        },
        "vendorName": "AVT-Company",
        "firmwareVersion": "0.9.87"
      },
      "reason": "PowerUp"
    }]);

    console.log('ws connected');

    this.myWebSocket.next(BN);
  }

  send_heartbeat() {
    sessionStorage.setItem('LastAction', "Heartbeat");
    var HB = JSON.stringify([2, this._id, "Heartbeat", {}]);
    this.myWebSocket.next(HB);
  }

  statusNotification(connectorStatus : ConnectorStatusEnumType, evseId : number, connectorId : number) {
    console.log("this is the evseid : " + evseId);
    sessionStorage.setItem('LastAction', "StatusNotification");
    var SN = [2, this._id, "StatusNotification", {
      "timestamp": this.formatDate(new Date()),
      "connectorStatus": connectorStatus,
      "evseId": evseId,
      "connectorId": connectorId
    }];
    this.myWebSocket.next(SN);
  }

  private handleData(ddata: any): any {
    // Your function implementation
  }

  private startHB(hb_interval: number): void {
    // Your function implementation
  }

  meterValues() {
    sessionStorage.setItem('LastAction', "MeterValues");
    var MV = JSON.stringify([2, this._id, "MeterValues", {
      "evseId": 2,
      "meterValue": [{
        "timestamp": this.formatDate(new Date()),
        "sampledValue": [{
          "value": "0",
          "measurand": "Energy.Active.Import.Register"
        }, {
          "value": "7200",
          "measurand": "Power_Active_Import"
        }]
      }, {
        "timestamp": this.formatDate(new Date()),
        "sampledValue": [{
          "value": "800",
          "measurand": "Energy.Active.Import.Register"
        }, {
          "value": "20",
          "measurand": "Energy.Reactive.Import.Register"
        }]
      }, {
        "timestamp": this.formatDate(new Date()),
        "sampledValue": [{
          "value": "42",
          "measurand": "SoC"
        }]
      }]
    }]);
    this.myWebSocket.next(MV);
  }

  firmwareStatusNotification() {

    sessionStorage.setItem('LastAction', "FirmwareStatusNotification");
    var FirmStat = JSON.stringify([2, this._id, "FirmwareStatusNotification", {
      "status": "Installed",
      "requestId": "0"
    }]);
    this.myWebSocket.next(FirmStat);

  }

  

  getLastAction() {
    var LastAction = sessionStorage.getItem("LastAction");
    return LastAction;
  }

  // Stop Transaction Request
  stopTransaction( request : TransactionEventRequest ,transaction_id: any = this._lastTranId) {
    sessionStorage.setItem('LastAction', "stopTransaction");
    transaction_id == false ? this._lastTranId = this._lastTranId : this._lastTranId = transaction_id.toString();
    this._connector_locked = false;
    console.log("Connector status changed to: " + this._connector_locked);
    var stpT = ([2, this._id, "TransactionEvent", request]);
    this.myWebSocket.next(stpT);
  }

  // Starting Transaction
  startTransaction(request : TransactionEventRequest) {
    sessionStorage.setItem('LastAction', "startTransaction");
    this._connector_locked = true;
    console.log("Connector status changed to: " + this._connector_locked);
    console.log(this._lastTranId);
    var strtT = ([2, this._id, "TransactionEvent", request]);
    this.myWebSocket.next(strtT);
  }

  formatDate(date : Date) {

    var day = String(date.getUTCDate());
    if (day.length < 2) {
      day = ('0' + day.slice(-2));
    }

    var monthIndex = String(date.getUTCMonth() + 1);
    if (monthIndex.length < 2) {
      monthIndex = ('0' + monthIndex.slice(-2));
    }
    var year = date.getUTCFullYear();
    var h = String(date.getUTCHours());
    var m = String(date.getUTCMinutes());
    var s = String(date.getUTCSeconds());

    if (h.length < 2) {
      h = ('0' + h.slice(-2));
    }
    if (m.length < 2) {
      m = ('0' + m.slice(-2));
    }
    if (s.length < 2) {
      s = ('0' + s.slice(-2));
    }
    return year + '-' + monthIndex + '-' + day + "T" + h + ":" + m + ":" + s + "Z";
  }

  


}
