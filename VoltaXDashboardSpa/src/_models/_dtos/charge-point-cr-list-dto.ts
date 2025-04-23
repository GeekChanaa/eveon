export interface ChargePointCRListDto{
    chargePointId : string,
    chargingStationName : string,
    serialNumber : string,
    category : string,
    status : string,
    partnerName : string,
    [key: string]: any;
}