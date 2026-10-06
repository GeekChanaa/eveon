using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    /// <summary>
    /// Availability of a station, derived from the live status of its connectors
    /// (only the connectors matching the connector type filter, when one is given).
    /// </summary>
    public enum StationAvailability
    {
        /// <summary>At least one connector is available.</summary>
        Available,
        /// <summary>No connector is available, at least one is occupied or reserved.</summary>
        Occupied,
        /// <summary>Every connector is unavailable, faulted or disconnected, or the station itself is out of service.</summary>
        Unavailable
    }

    public class ChargingStationSearchParams
    {
        /// <summary>Free text matched against address, city, state, country and station name. Every word must match.</summary>
        public string? Query { get; set; }
        /// <summary>Several values are allowed: ?availability=Available&amp;availability=Occupied</summary>
        public List<StationAvailability>? Availability { get; set; }
        /// <summary>Several values are allowed: ?connectorTypes=cType2&amp;connectorTypes=cCCS2</summary>
        public List<ConnectorEnumType>? ConnectorTypes { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ChargingStationSearchResultDto
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public StationAvailability Availability { get; set; }
        public List<ConnectorEnumType> ConnectorTypes { get; set; } = new();
        public int TotalConnectors { get; set; }
        public int AvailableConnectors { get; set; }
        public int OccupiedConnectors { get; set; }
        public int UnavailableConnectors { get; set; }
        public List<ChargePointSearchResultDto> ChargePoints { get; set; } = new();
    }

    public class ChargePointSearchResultDto
    {
        public int ID { get; set; }
        public string? ChargePointId { get; set; }
        public List<ConnectorSearchResultDto> Connectors { get; set; } = new();
    }

    public class ConnectorSearchResultDto
    {
        public int ID { get; set; }
        public int? ConnectorID { get; set; }
        public int EvseID { get; set; }
        public ConnectorEnumType? ConnectorType { get; set; }
        public double PowerKw { get; set; }
        /// <summary>Latest reported status; Disconnected when the connector never reported one.</summary>
        public ConnectorStatusEnumType Status { get; set; }
    }

    public class ChargingStationSearchResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ChargingStationSearchResultDto> Items { get; set; } = new();
    }
}
