export interface ChargingSessionListDto{
    id : number,
    connectorID : number,
    connector : string,
    chargePointID : number,
    userName : string,
    cardNumber : string,
    startDate : Date,
    endDate : Date,
    stoppedReason : string,
    chargingSessionStatus : string,
    [key: string]: any;
}