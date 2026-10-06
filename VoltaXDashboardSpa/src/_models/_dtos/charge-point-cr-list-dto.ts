export interface ChargePointCRListDto{
    chargePointId : string,
    chargingStationName : string,
    serialNumber : string,
    category : string,
    status : string,
    partnerName : string,
    /** Configured by VoltaX (automatic or manual setup). */
    isConfigured? : boolean,
    configurationMethod? : 'Automatic' | 'Manual' | 'Skipped' | 'Legacy' | null,
    isOnline? : boolean,
    /** "ocpp1.6" | "ocpp2.0.1", null when not connected. */
    protocolVersion? : string | null,
    // Display columns filled in by the list page.
    connection? : string,
    configuration? : string,
    ocpp? : string,
    [key: string]: any;
}