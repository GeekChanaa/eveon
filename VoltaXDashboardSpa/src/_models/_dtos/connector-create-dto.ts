export interface ConnectorCreateDto {
    connectorType?: string;
    speed: number;
    pricePerKWh : number;
    pricePerMinute : number;
    pricePerHour : number;
}
  