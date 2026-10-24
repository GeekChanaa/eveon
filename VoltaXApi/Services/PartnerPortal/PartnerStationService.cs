using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Services.PartnerPortal
{
    /// <summary>Another station (of any partner, deleted ones included) already has the name.</summary>
    public sealed class StationNameTakenException() : Exception("Another station already has this name.");

    /// <summary>
    /// The public information of the partner's own stations: name, address, location, access, opening hours,
    /// description, amenities and photos. Every method is scoped to the partner id passed in (a station of another
    /// partner is "not found"); every change is audited. Status, charge points and ownership stay with operators.
    /// </summary>
    public sealed class PartnerStationService
    {
        public const int MaxPhotos = 8;
        public const int MaxPhotosPerUpload = 5;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly VoltaXApiDbContext _db;
        private readonly IFileManagementService _files;
        private readonly IAuditLogger _audit;

        public PartnerStationService(VoltaXApiDbContext db, IFileManagementService files, IAuditLogger audit)
        {
            _db = db;
            _files = files;
            _audit = audit;
        }

        public async Task<PartnerStationDetailDto?> StationAsync(int partnerId, int stationId, CancellationToken ct = default)
        {
            var station = await _db.ChargingStations.AsNoTracking().FirstOrDefaultAsync(s => s.ID == stationId && s.PartnerID == partnerId, ct);
            if (station == null)
                return null;
            var photos = await _db.ChargingStationImages.AsNoTracking().Where(i => i.ChargingStationID == stationId && !i.Image!.IsDeleted)
                .OrderBy(i => i.ID).Select(i => new PartnerStationPhotoDto(i.ID, i.Image!.Url)).ToListAsync(ct);
            var points = await _db.ChargePoints.AsNoTracking().Where(cp => cp.ChargingStationID == stationId)
                .Select(cp => cp.Connectors!.Count(c => !c.IsDeleted)).ToListAsync(ct);
            return new PartnerStationDetailDto(station.ID, station.Name, station.Address, station.City, Coordinate(station.Latitude),
                Coordinate(station.Longitude), station.Status, station.Network, station.ParkingType, station.Description, Hours(station.OpeningHours),
                new PartnerStationAmenities(station.WifiAmenity, station.ParkingAmenity, station.RestaurantsAmenity, station.WashroomAmenity, station.SittingAreaAmenity),
                photos, points.Count, points.Sum(), station.UpdatedAt);
        }

        /// <summary>Saves the station's public information; null when the station is not the partner's.</summary>
        /// <exception cref="StationNameTakenException">Another station already has this name.</exception>
        public async Task<PartnerStationDetailDto?> UpdateAsync(int partnerId, int stationId, UpdatePartnerStationRequest request, CancellationToken ct = default)
        {
            var station = await _db.ChargingStations.FirstOrDefaultAsync(s => s.ID == stationId && s.PartnerID == partnerId, ct);
            if (station == null)
                return null;
            var name = request.Name.Trim();
            // Names are unique across all stations, deleted ones included (unique index).
            if (await _db.ChargingStations.IgnoreQueryFilters().AnyAsync(s => s.ID != stationId && s.Name == name, ct))
                throw new StationNameTakenException();

            var before = Snapshot(station);
            station.Name = name;
            station.Address = request.Address.Trim();
            station.City = request.City.Trim();
            station.Latitude = request.Latitude?.ToString("0.######", CultureInfo.InvariantCulture);
            station.Longitude = request.Longitude?.ToString("0.######", CultureInfo.InvariantCulture);
            station.Network = request.Access!.Value;
            if (request.ParkingType.HasValue)
                station.ParkingType = request.ParkingType.Value;
            station.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            station.OpeningHours = JsonSerializer.Serialize(Normalized(request.OpeningHours!), Json);
            if (request.Amenities is { } amenities)
            {
                station.WifiAmenity = amenities.Wifi;
                station.ParkingAmenity = amenities.Parking;
                station.RestaurantsAmenity = amenities.Restaurants;
                station.WashroomAmenity = amenities.Washroom;
                station.SittingAreaAmenity = amenities.SittingArea;
            }
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("PartnerPortal.UpdateStation", nameof(ChargingStation), station.ID.ToString(CultureInfo.InvariantCulture),
                new { before, after = Snapshot(station) });
            return await StationAsync(partnerId, stationId, ct);
        }

        /// <summary>Adds photos (images checked by content and size); null when the station is not the partner's.</summary>
        /// <exception cref="ValidationException">No file, too many photos, or a file that is not an image.</exception>
        public async Task<PartnerStationDetailDto?> AddPhotosAsync(int partnerId, int stationId, IReadOnlyList<IFormFile> files, CancellationToken ct = default)
        {
            if (!await _db.ChargingStations.AnyAsync(s => s.ID == stationId && s.PartnerID == partnerId, ct))
                return null;
            if (files.Count == 0)
                throw new ValidationException("Choose at least one photo.");
            if (files.Count > MaxPhotosPerUpload)
                throw new ValidationException($"Upload at most {MaxPhotosPerUpload} photos at a time.");
            var existing = await _db.ChargingStationImages.CountAsync(i => i.ChargingStationID == stationId && !i.Image!.IsDeleted, ct);
            if (existing + files.Count > MaxPhotos)
                throw new ValidationException($"A station has at most {MaxPhotos} photos.");

            // Every file is checked before any is stored, so a rejected one leaves the station unchanged.
            foreach (var file in files)
                ImageUploadValidator.Validate(file);

            var added = new List<int>();
            foreach (var file in files)
            {
                var stored = await _files.SaveImageAsync(file, "images/charging-stations", ct);
                var image = new Image
                {
                    Url = stored.RelativeUrl, UploadDate = DateTime.UtcNow, Format = stored.Extension, Priority = ImagePriorityEnum.Principal,
                    IsActive = true, AltText = "Charging Station Image " + stationId, Description = "none"
                };
                var link = new ChargingStationImage { ChargingStationID = stationId, Image = image };
                _db.ChargingStationImages.Add(link);
                await _db.SaveChangesAsync(ct);
                added.Add(link.ID);
            }
            await _audit.LogAsync("PartnerPortal.AddStationPhotos", nameof(ChargingStation), stationId.ToString(CultureInfo.InvariantCulture), new { photos = added });
            return await StationAsync(partnerId, stationId, ct);
        }

        /// <summary>Removes one photo of the partner's station; false when the station or the photo is not theirs.</summary>
        public async Task<bool> RemovePhotoAsync(int partnerId, int stationId, int photoId, CancellationToken ct = default)
        {
            var link = await _db.ChargingStationImages.Include(i => i.Image)
                .FirstOrDefaultAsync(i => i.ID == photoId && i.ChargingStationID == stationId && i.ChargingStation!.PartnerID == partnerId, ct);
            if (link == null)
                return false;
            _db.ChargingStationImages.Remove(link); // soft delete
            if (link.Image != null)
                _db.Remove(link.Image);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("PartnerPortal.RemoveStationPhoto", nameof(ChargingStation), stationId.ToString(CultureInfo.InvariantCulture),
                new { photo = photoId, url = link.Image?.Url });
            return true;
        }

        /// <summary>The stored opening hours; stations without (or with unreadable) hours are shown as always open.</summary>
        public static StationOpeningHours Hours(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return StationOpeningHours.AlwaysOpen;
            try
            {
                return JsonSerializer.Deserialize<StationOpeningHours>(json, Json) ?? StationOpeningHours.AlwaysOpen;
            }
            catch (JsonException)
            {
                return StationOpeningHours.AlwaysOpen;
            }
        }

        private static StationOpeningHours Normalized(StationOpeningHours hours) => hours.TwentyFourSeven
            ? StationOpeningHours.AlwaysOpen
            : new StationOpeningHours
            {
                TwentyFourSeven = false,
                Regular = hours.Regular.OrderBy(p => p.Weekday).ThenBy(p => p.PeriodBegin, StringComparer.Ordinal)
                    .Select(p => new StationOpeningPeriod { Weekday = p.Weekday, PeriodBegin = p.PeriodBegin, PeriodEnd = p.PeriodEnd }).ToList()
            };

        private static double? Coordinate(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;

        private static object Snapshot(ChargingStation s) => new
        {
            s.Name, s.Address, s.City, s.Latitude, s.Longitude, Access = s.Network.ToString(), ParkingType = s.ParkingType.ToString(), s.Description,
            s.OpeningHours, s.WifiAmenity, s.ParkingAmenity, s.RestaurantsAmenity, s.WashroomAmenity, s.SittingAreaAmenity
        };
    }
}
