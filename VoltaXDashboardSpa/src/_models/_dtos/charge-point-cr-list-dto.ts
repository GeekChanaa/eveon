export interface ChargePointCRListDto{
    chargePointId : string,
    name : string,
    chargingStationName : string,
    serialNumber : string,
    category : string,
    status : string,
    partnerName : string,
    [key: string]: any;
}