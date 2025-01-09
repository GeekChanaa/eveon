
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Helpers;

public static class ConnectorStatusHelper
{
  public static ConnectorStatusEnumType ConvertToEnum(string status)
  {
      if (Enum.TryParse<ConnectorStatusEnumType>(status, true, out var parsedStatus))
      {
          return parsedStatus;
      }
      return ConnectorStatusEnumType.Unavailable;
  }

  public static ConnectorUptimeStatusEnum ConvertConnectorStatustoConnectorUptimeStatus(ConnectorStatusEnumType status)
  {
    switch(status){
      case ConnectorStatusEnumType.Available :
        return  ConnectorUptimeStatusEnum.Available;
      case ConnectorStatusEnumType.Occupied :
        return  ConnectorUptimeStatusEnum.SuspendedEV;
      case ConnectorStatusEnumType.Reserved :
        return  ConnectorUptimeStatusEnum.Reserved;
      case ConnectorStatusEnumType.Faulted :
        return  ConnectorUptimeStatusEnum.Faulted;
      default : 
        return ConnectorUptimeStatusEnum.Unavailable;
    }

  }

}