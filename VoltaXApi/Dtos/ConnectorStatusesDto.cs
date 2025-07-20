namespace VoltaXApi.Dtos
{
    public class ConnectorStatusesDto
    {
      public int NbrAvailableConnectors { get; set; }
      public int NbrOccupiedConnectors { get; set; }
      public int NbrReservedConnectors { get; set; }
      public int NbrUnavailableConnectors { get; set; }
      public int NbrFaultedConnectors { get; set; }
      public int NbrDisconnectedConnectors { get; set; }
    }
}