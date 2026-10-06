using System.Globalization;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Ocpi.Services
{
    public sealed record OcpiIdentity(string CountryCode, string PartyId, string OperatorName, double VatRate);

    public sealed record OcpiCost(decimal Energy, decimal Time, decimal Fixed, decimal TotalInclVat, decimal TotalExclVat);

    // Maps EVEON entities to OCPI 2.2.1 objects. Ids are derived from database ids so they never change:
    //  Location = ChargingStation.ID, EVSE uid = "{ChargePoint.ID}-{OCPP evse id}",
    //  Connector id = OCPP connector id, Tariff id = Connector.ID, Session/CDR id = OcpiSession.ID.
    public static class OcpiMapper
    {
        public const string DefaultCountryAlpha3 = "MAR";

        public static string LocationId(int chargingStationId) => chargingStationId.ToString(CultureInfo.InvariantCulture);
        public static string EvseUid(int chargePointId, int evseId) => $"{chargePointId}-{evseId}";
        public static string EvseId(OcpiIdentity id, int chargePointId, int evseId) => $"{id.CountryCode}*{id.PartyId}*E{chargePointId}*{evseId}";
        public static string ConnectorId(Connector connector) => (connector.ConnectorID ?? 1).ToString(CultureInfo.InvariantCulture);
        public static string TariffId(Connector connector) => connector.ID.ToString(CultureInfo.InvariantCulture);

        public static bool TryParseEvseUid(string? uid, out int chargePointId, out int evseId)
        {
            chargePointId = evseId = 0;
            var parts = uid?.Split('-');
            return parts is { Length: 2 } && int.TryParse(parts[0], out chargePointId) && int.TryParse(parts[1], out evseId);
        }

        public static bool TryParseId(string? value, out int id) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;

        public static DateTime Max(params DateTime?[] values) =>
            OcpiJson.ToUtc(values.Where(v => v.HasValue).Select(v => OcpiJson.ToUtc(v!.Value)).DefaultIfEmpty(DateTime.UnixEpoch).Max());

        // Only public stations and charge points shown on the map are offered for roaming.
        public static bool IsPublished(ChargingStation station) =>
            !station.IsDeleted && station.Network == ChargingStationNetworkEnum.Public && station.Category != ChargingStationCategoryEnum.Private;

        public static bool IsPublished(ChargePoint chargePoint) => !chargePoint.IsDeleted && chargePoint.ShowOnMap != false;

        // Null when the station cannot be published (OCPI requires coordinates).
        public static GeoLocationDto? Coordinates(string? latitude, string? longitude)
        {
            if (!double.TryParse(latitude?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(longitude?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) ||
                lat is < -90 or > 90 || lon is < -180 or > 180 || lat == 0 && lon == 0)
                return null;
            return new GeoLocationDto
            {
                Latitude = lat.ToString("0.000000", CultureInfo.InvariantCulture),
                Longitude = lon.ToString("0.000000", CultureInfo.InvariantCulture)
            };
        }

        public static string CountryAlpha3(string? country)
        {
            var value = country?.Trim().ToUpperInvariant();
            return value is { Length: 3 } && value.All(char.IsLetter) ? value : DefaultCountryAlpha3;
        }

        public static LocationDto? ToLocation(OcpiIdentity id, ChargingStation station, IReadOnlyDictionary<int, ConnectorStatus> statuses)
        {
            var coordinates = Coordinates(station.Latitude, station.Longitude);
            if (coordinates == null) return null;
            var evses = ToEvses(id, station, statuses);
            var facilities = new List<string>();
            if (station.RestaurantsAmenity) facilities.Add("RESTAURANT");
            if (station.WifiAmenity) facilities.Add("WIFI");
            return new LocationDto
            {
                CountryCode = id.CountryCode,
                PartyId = id.PartyId,
                Id = LocationId(station.ID),
                Publish = true,
                Name = station.Name,
                Address = Truncate(station.Address, 45),
                City = Truncate(station.City, 45),
                State = string.IsNullOrWhiteSpace(station.State) ? null : Truncate(station.State, 20),
                Country = CountryAlpha3(station.Country),
                Coordinates = coordinates,
                Evses = evses,
                Operator = new BusinessDetailsDto { Name = id.OperatorName },
                Facilities = facilities.Count > 0 ? facilities : null,
                TimeZone = OcpiOptions.TimeZone,
                OpeningTimes = new HoursDto { TwentyFourSeven = true },
                LastUpdated = Max(new DateTime?[] { station.UpdatedAt }.Concat(evses.Select(e => (DateTime?)e.LastUpdated)).ToArray())
            };
        }

        public static List<EvseDto> ToEvses(OcpiIdentity id, ChargingStation station, IReadOnlyDictionary<int, ConnectorStatus> statuses) =>
            (station.ChargePoints ?? new List<ChargePoint>())
                .OrderBy(cp => cp.ID)
                .SelectMany(cp => (cp.Connectors ?? new List<Connector>())
                    .GroupBy(c => c.EvseID)
                    .OrderBy(g => g.Key)
                    .Select(g => ToEvse(id, station, cp, g.Key, g.ToList(), statuses)))
                .ToList();

        public static EvseDto ToEvse(OcpiIdentity id, ChargingStation station, ChargePoint chargePoint, int evseId,
            IReadOnlyCollection<Connector> connectors, IReadOnlyDictionary<int, ConnectorStatus> statuses)
        {
            var live = connectors.Where(c => !c.IsDeleted).OrderBy(c => c.ConnectorID).ToList();
            var removed = !IsPublished(station) || !IsPublished(chargePoint) || live.Count == 0;
            var connectorStatuses = live.Select(c => statuses.TryGetValue(c.ID, out var s) ? s : null).ToList();
            return new EvseDto
            {
                Uid = EvseUid(chargePoint.ID, evseId),
                EvseId = EvseId(id, chargePoint.ID, evseId),
                Status = removed ? "REMOVED" : EvseStatus(station.Status, chargePoint.Status, connectorStatuses.Select(s => s?.LastStatus)),
                Capabilities = new List<string> { "REMOTE_START_STOP_CAPABLE", "RESERVABLE", "RFID_READER", "UNLOCK_CAPABLE" },
                Connectors = (removed ? connectors.OrderBy(c => c.ConnectorID).ToList() : live).Select(c => ToConnector(c)).ToList(),
                LastUpdated = Max(new DateTime?[] { chargePoint.UpdatedAt }
                    .Concat(connectors.Select(c => (DateTime?)c.UpdatedAt))
                    .Concat(connectorStatuses.Select(s => s?.UpdatedAt)).ToArray())
            };
        }

        // One OCPI status for the EVSE from the last OCPP status of each of its connectors.
        public static string EvseStatus(ChargingStationStatusEnum stationStatus, ChargePointStatusEnum chargePointStatus,
            IEnumerable<ConnectorStatusEnumType?> connectorStatuses)
        {
            if (stationStatus == ChargingStationStatusEnum.UnderMaintenance || chargePointStatus == ChargePointStatusEnum.UnderMaintenance)
                return "INOPERATIVE";
            if (stationStatus == ChargingStationStatusEnum.Offline || chargePointStatus == ChargePointStatusEnum.Offline)
                return "UNKNOWN";
            var list = connectorStatuses.ToList();
            if (list.Contains(ConnectorStatusEnumType.Occupied)) return "CHARGING";
            if (list.Contains(ConnectorStatusEnumType.Reserved)) return "RESERVED";
            if (list.Contains(ConnectorStatusEnumType.Available)) return "AVAILABLE";
            if (list.Contains(ConnectorStatusEnumType.Faulted)) return "OUTOFORDER";
            if (list.Contains(ConnectorStatusEnumType.Unavailable)) return "INOPERATIVE";
            return "UNKNOWN";
        }

        public static (string Standard, string Format) ConnectorKind(ConnectorEnumType? type) => type switch
        {
            ConnectorEnumType.cCCS1 => ("IEC_62196_T1_COMBO", "CABLE"),
            ConnectorEnumType.cCCS2 => ("IEC_62196_T2_COMBO", "CABLE"),
            ConnectorEnumType.cG105 => ("CHADEMO", "CABLE"),
            ConnectorEnumType.cTesla => ("TESLA_S", "CABLE"),
            ConnectorEnumType.cType1 => ("IEC_62196_T1", "CABLE"),
            ConnectorEnumType.cType2 => ("IEC_62196_T2", "CABLE"),
            ConnectorEnumType.s309_1P_16A => ("IEC_60309_2_single_16", "SOCKET"),
            // OCPI 2.2.1 has no single phase 32 A type; the rating is still in max_amperage.
            ConnectorEnumType.s309_1P_32A => ("IEC_60309_2_single_16", "SOCKET"),
            ConnectorEnumType.s309_3P_16A => ("IEC_60309_2_three_16", "SOCKET"),
            ConnectorEnumType.s309_3P_32A => ("IEC_60309_2_three_32", "SOCKET"),
            ConnectorEnumType.sBS1361 => ("DOMESTIC_G", "SOCKET"),
            ConnectorEnumType.sCEE_7_7 => ("DOMESTIC_F", "SOCKET"),
            ConnectorEnumType.sType2 => ("IEC_62196_T2", "SOCKET"),
            ConnectorEnumType.sType3 => ("IEC_62196_T3C", "SOCKET"),
            ConnectorEnumType.Pan => ("PANTOGRAPH_BOTTOM_UP", "CABLE"),
            _ => ("IEC_62196_T2", "SOCKET")
        };

        public static bool IsDc(ConnectorEnumType? type) =>
            type is ConnectorEnumType.cCCS1 or ConnectorEnumType.cCCS2 or ConnectorEnumType.cG105 or ConnectorEnumType.Pan;

        // Connector.Power is in kW; 0 means "not configured", then MaxPower is used.
        public static ConnectorDto ToConnector(Connector connector)
        {
            var (standard, format) = ConnectorKind(connector.ConnectorType);
            var kw = connector.Power > 0 ? connector.Power : connector.MaxPower > 0 ? connector.MaxPower : 22;
            string powerType;
            int voltage, amperage;
            if (IsDc(connector.ConnectorType))
            {
                powerType = "DC";
                voltage = kw > 150 ? 920 : 500;
                amperage = (int)Math.Ceiling(kw * 1000 / voltage);
            }
            else if (connector.ConnectorType == ConnectorEnumType.cType1 || kw <= 7.4)
            {
                powerType = "AC_1_PHASE";
                voltage = 230;
                amperage = (int)Math.Ceiling(kw * 1000 / 230);
            }
            else
            {
                // OCPI 2.2.1: AC_3_PHASE voltage is line to neutral, amperage per phase.
                powerType = "AC_3_PHASE";
                voltage = 230;
                amperage = (int)Math.Ceiling(kw * 1000 / (3 * 230));
            }
            return new ConnectorDto
            {
                Id = ConnectorId(connector),
                Standard = standard,
                Format = format,
                PowerType = powerType,
                MaxVoltage = voltage,
                MaxAmperage = amperage,
                MaxElectricPower = (int)Math.Round(kw * 1000),
                TariffIds = new List<string> { TariffId(connector) },
                LastUpdated = OcpiJson.ToUtc(connector.UpdatedAt)
            };
        }

        public static decimal ExclVat(double priceInclVat, double vatRate) =>
            Math.Round((decimal)priceInclVat / (1 + (decimal)vatRate), 4, MidpointRounding.AwayFromZero);

        // EVEON prices include VAT; OCPI prices exclude it and carry the VAT percentage.
        public static TariffDto ToTariff(OcpiIdentity id, Connector connector)
        {
            var vat = Math.Round((decimal)id.VatRate * 100, 2);
            var components = new List<PriceComponentDto>();
            if (connector.FlatFee > 0)
                components.Add(new PriceComponentDto { Type = "FLAT", Price = ExclVat(connector.FlatFee, id.VatRate), Vat = vat, StepSize = 1 });
            if (connector.PricePerKWh > 0)
                components.Add(new PriceComponentDto { Type = "ENERGY", Price = ExclVat(connector.PricePerKWh, id.VatRate), Vat = vat, StepSize = 1 });
            if (connector.PricePerMinute > 0)
                components.Add(new PriceComponentDto { Type = "TIME", Price = ExclVat(connector.PricePerMinute * 60, id.VatRate), Vat = vat, StepSize = 60 });
            if (connector.PricePerIdleMinute > 0)
                components.Add(new PriceComponentDto { Type = "PARKING_TIME", Price = ExclVat(connector.PricePerIdleMinute * 60, id.VatRate), Vat = vat, StepSize = 60 });
            if (components.Count == 0)
                components.Add(new PriceComponentDto { Type = "FLAT", Price = 0, Vat = vat, StepSize = 1 });
            return new TariffDto
            {
                CountryCode = id.CountryCode,
                PartyId = id.PartyId,
                Id = TariffId(connector),
                Currency = OcpiOptions.Currency,
                Type = "REGULAR",
                Elements = new List<TariffElementDto> { new() { PriceComponents = components } },
                LastUpdated = OcpiJson.ToUtc(connector.UpdatedAt)
            };
        }

        public static OcpiCost Cost(OcpiSession session, DateTime? end = null)
        {
            var stop = session.EndDateTime ?? end ?? DateTime.UtcNow;
            var minutes = Math.Max(0, (OcpiJson.ToUtc(stop) - OcpiJson.ToUtc(session.StartDateTime)).TotalMinutes);
            var energy = (decimal)(session.Kwh * session.PricePerKWh);
            var time = (decimal)(Math.Ceiling(minutes) * session.PricePerMinute);
            var fixedFee = (decimal)session.FlatFee;
            var incl = Math.Round(energy + time + fixedFee, 2, MidpointRounding.AwayFromZero);
            var excl = Math.Round(incl / (1 + (decimal)session.VatRate), 2, MidpointRounding.AwayFromZero);
            return new OcpiCost(energy, time, fixedFee, incl, excl);
        }

        public static PriceDto Price(decimal inclVat, double vatRate) => new()
        {
            ExclVat = Math.Round(inclVat / (1 + (decimal)vatRate), 2, MidpointRounding.AwayFromZero),
            InclVat = Math.Round(inclVat, 2, MidpointRounding.AwayFromZero)
        };

        public static CdrTokenDto CdrToken(OcpiSession session) => new()
        {
            CountryCode = session.TokenCountryCode,
            PartyId = session.TokenPartyId,
            Uid = session.TokenUid,
            Type = session.TokenType,
            ContractId = session.ContractId
        };

        public static string SessionStatus(OcpiSessionStatus status) => status switch
        {
            OcpiSessionStatus.Active => "ACTIVE",
            OcpiSessionStatus.Completed => "COMPLETED",
            OcpiSessionStatus.Invalid => "INVALID",
            _ => "PENDING"
        };

        public static SessionDto ToSession(OcpiIdentity id, OcpiSession session, Connector? connector)
        {
            var cost = Cost(session);
            return new SessionDto
            {
                CountryCode = id.CountryCode,
                PartyId = id.PartyId,
                Id = session.ID.ToString(CultureInfo.InvariantCulture),
                StartDateTime = OcpiJson.ToUtc(session.StartDateTime),
                EndDateTime = session.EndDateTime.HasValue ? OcpiJson.ToUtc(session.EndDateTime.Value) : null,
                Kwh = Math.Round((decimal)session.Kwh, 3),
                CdrToken = CdrToken(session),
                AuthMethod = session.AuthMethod,
                AuthorizationReference = session.AuthorizationReference,
                LocationId = LocationId(session.ChargingStationID),
                EvseUid = EvseUid(session.ChargePointID, session.EvseId),
                ConnectorId = connector != null ? ConnectorId(connector) : "1",
                Currency = OcpiOptions.Currency,
                TotalCost = session.Status == OcpiSessionStatus.Pending ? null : new PriceDto { ExclVat = cost.TotalExclVat, InclVat = cost.TotalInclVat },
                Status = SessionStatus(session.Status),
                LastUpdated = OcpiJson.ToUtc(session.LastUpdated)
            };
        }

        public static CdrDto ToCdr(OcpiIdentity id, OcpiSession session, ChargingStation station, ChargePoint chargePoint, Connector connector)
        {
            var end = OcpiJson.ToUtc(session.EndDateTime ?? session.LastUpdated);
            var start = OcpiJson.ToUtc(session.StartDateTime);
            var hours = Math.Round((decimal)Math.Max(0, (end - start).TotalHours), 4);
            var cost = Cost(session, end);
            var tariff = ToTariff(id, new Connector
            {
                ID = connector.ID,
                PricePerKWh = session.PricePerKWh,
                PricePerMinute = session.PricePerMinute,
                PricePerIdleMinute = session.PricePerIdleMinute,
                FlatFee = session.FlatFee,
                UpdatedAt = session.StartDateTime
            });
            var (standard, format) = ConnectorKind(connector.ConnectorType);
            var connectorDto = ToConnector(connector);
            return new CdrDto
            {
                CountryCode = id.CountryCode,
                PartyId = id.PartyId,
                Id = session.ID.ToString(CultureInfo.InvariantCulture),
                StartDateTime = start,
                EndDateTime = end,
                SessionId = session.ID.ToString(CultureInfo.InvariantCulture),
                CdrToken = CdrToken(session),
                AuthMethod = session.AuthMethod,
                AuthorizationReference = session.AuthorizationReference,
                CdrLocation = new CdrLocationDto
                {
                    Id = LocationId(station.ID),
                    Name = station.Name,
                    Address = Truncate(station.Address, 45),
                    City = Truncate(station.City, 45),
                    State = string.IsNullOrWhiteSpace(station.State) ? null : Truncate(station.State, 20),
                    Country = CountryAlpha3(station.Country),
                    Coordinates = Coordinates(station.Latitude, station.Longitude) ?? new GeoLocationDto { Latitude = "0.000000", Longitude = "0.000000" },
                    EvseUid = EvseUid(chargePoint.ID, session.EvseId),
                    EvseId = EvseId(id, chargePoint.ID, session.EvseId),
                    ConnectorId = ConnectorId(connector),
                    ConnectorStandard = standard,
                    ConnectorFormat = format,
                    ConnectorPowerType = connectorDto.PowerType
                },
                Currency = OcpiOptions.Currency,
                Tariffs = new List<TariffDto> { tariff },
                ChargingPeriods = new List<ChargingPeriodDto>
                {
                    new()
                    {
                        StartDateTime = start,
                        TariffId = tariff.Id,
                        Dimensions = new List<CdrDimensionDto>
                        {
                            new() { Type = "ENERGY", Volume = Math.Round((decimal)session.Kwh, 3) },
                            new() { Type = "TIME", Volume = hours }
                        }
                    }
                },
                TotalCost = new PriceDto { ExclVat = cost.TotalExclVat, InclVat = cost.TotalInclVat },
                TotalFixedCost = cost.Fixed > 0 ? Price(cost.Fixed, session.VatRate) : null,
                TotalEnergy = Math.Round((decimal)session.Kwh, 3),
                TotalEnergyCost = cost.Energy > 0 ? Price(cost.Energy, session.VatRate) : null,
                TotalTime = hours,
                TotalTimeCost = cost.Time > 0 ? Price(cost.Time, session.VatRate) : null,
                LastUpdated = OcpiJson.ToUtc(session.LastUpdated)
            };
        }

        private static string Truncate(string? value, int length)
        {
            var text = value?.Trim() ?? "";
            return text.Length <= length ? text : text[..length];
        }
    }
}
