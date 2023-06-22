export interface ConnectorStatus{
    id : number;
    chargePointId : number;
    connectorId : number;
    lastStatus : string;
    lastStatusTime : Date 
}