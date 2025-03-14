export interface OCPPConfigurationItem{
    id : number,
    chargePointID : number,
    ocppConfigurationComponentID : number,
    ocppConfigurationVariableID : number,
    ocppConfigurationComponent : any,
    ocppConfigurationVariable : any
    ocppConfigurationVariableAttributes : any,
    ocppConfigurationVariableCharacteristicID : number,
    ocppConfigurationVariableCharacteristic : number
    chargePoint : any,
    [key: string]: any;
}