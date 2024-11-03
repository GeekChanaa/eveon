
export interface ChargePointUptime {
    id: number;
    chargePointID: number;
    startDate : Date;
    endDate : Date;
    chargePointUptimeStatus : Date;
    [key: string]: any;
  }
  