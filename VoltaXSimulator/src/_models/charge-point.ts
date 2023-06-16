import { Connector } from "./connector";

export class ChargePoint {
    constructor(
      public chargePointId: string,
      public chargeStationId: string,
      public chargePointName: string,
      public chargePointGateway: string,
      public connectors: Connector[]
    ) {}
  }