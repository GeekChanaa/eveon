export interface MessageLog{
  logTime : Date,
  chargePointId : string, 
  connectorId : number,
  message : string,
  result : string,
  errorCode : number,
  contentSent : string,
  contentReceived : string
}