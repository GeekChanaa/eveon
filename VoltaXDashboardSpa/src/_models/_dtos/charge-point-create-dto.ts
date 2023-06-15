

export interface ChargePointCreateDto {
    chargePointId: string;
    chargingStationID : number;
    name: string;
    serialNumber: string;
    make: string;
    status: string;
    comment: string;
    username: string;
    password: string;
    clientCertThumb: string;
  }
  