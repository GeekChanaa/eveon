export interface ConnectorStatus{
    id : number;
    chargePointID : number;
    connectorID : number;
    lastStatus : string;
    lastStatusTime : Date 
}