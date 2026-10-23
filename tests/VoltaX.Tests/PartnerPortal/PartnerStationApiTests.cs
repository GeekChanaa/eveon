using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.PartnerPortal;

/// <summary>A partner edits the public information and photos of their own stations only, validated and audited.</summary>
public class PartnerStationApiTests : IAsyncLifetime
{
    // 1x1 transparent PNG.
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private readonly ApiFactory _api = new();
    private User _partnerA = null!, _partnerB = null!, _customer = null!;
    private int _stationA, _stationB;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            var a = new Partner { Name = "Atlas Energy", Description = "", Email = "a@atlas.ma", Phone = "0600000001", PartnerIdentificationNumber = "P-A" };
            var b = new Partner { Name = "Borne Plus", Description = "", Email = "b@borne.ma", Phone = "0600000002", PartnerIdentificationNumber = "P-B" };
            db.Partners.AddRange(a, b);
            await db.SaveChangesAsync();
            _partnerA = await Seeds.User(db, "partner.a@test.ma", role: "Partner", partnerId: a.ID);
            _partnerB = await Seeds.User(db, "partner.b@test.ma", role: "Partner", partnerId: b.ID);
            _customer = await Seeds.User(db, "driver@test.ma");
            _stationA = (await Seeds.Station(db, a.ID, "Atlas Agdal")).ID;
            _stationB = (await Seeds.Station(db, b.ID, "Borne Maarif")).ID;
            await Seeds.ChargePoint(db, "CP-A1", c => c.PricePerKWh = 3m, _stationA);
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static object Info(string name = "Atlas Agdal Centre", object? hours = null, string? description = "Underground car park, level -1.",
        double? latitude = 33.99, double? longitude = -6.85) => new
    {
        name, address = "12 Avenue Fal Ould Oumeir", city = "Rabat", latitude, longitude, access = "Public", parkingType = "PerpendicularParking",
        description,
        openingHours = hours ?? new
        {
            twentyFourSeven = false,
            regular = new[]
            {
                new { weekday = 6, periodBegin = "09:00", periodEnd = "13:00" },
                new { weekday = 1, periodBegin = "08:00", periodEnd = "20:00" },
                new { weekday = 6, periodBegin = "14:00", periodEnd = "24:00" }
            }
        },
        amenities = new { wifi = true, parking = true, restaurants = false, washroom = true, sittingArea = false }
    };

    private static MultipartFormDataContent Photos(params byte[][] files)
    {
        var form = new MultipartFormDataContent();
        foreach (var bytes in files)
        {
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(file, "photos", "photo.png");
        }
        return form;
    }

    [Fact]
    public async Task A_partner_reads_their_station_with_its_public_information()
    {
        var station = await _api.Client(_partnerA).GetFromJsonAsync<JsonElement>($"/api/partner-portal/stations/{_stationA}");

        Assert.Equal("Atlas Agdal", station.GetProperty("name").GetString());
        Assert.True(station.GetProperty("openingHours").GetProperty("twentyFourSeven").GetBoolean()); // no hours yet: always open
        Assert.Equal(1, station.GetProperty("chargePointCount").GetInt32());
        Assert.Equal(0, station.GetProperty("photos").GetArrayLength());
    }

    [Fact]
    public async Task A_partner_edits_the_public_information_of_their_station_and_it_is_audited()
    {
        var response = await _api.Client(_partnerA).PutAsJsonAsync($"/api/partner-portal/stations/{_stationA}", Info());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var station = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Atlas Agdal Centre", station.GetProperty("name").GetString());
        Assert.Equal("Rabat", station.GetProperty("city").GetString());
        Assert.Equal(33.99, station.GetProperty("latitude").GetDouble());
        Assert.Equal("Underground car park, level -1.", station.GetProperty("description").GetString());
        var periods = station.GetProperty("openingHours").GetProperty("regular").EnumerateArray()
            .Select(p => $"{p.GetProperty("weekday").GetInt32()} {p.GetProperty("periodBegin").GetString()}-{p.GetProperty("periodEnd").GetString()}").ToArray();
        Assert.Equal(new[] { "1 08:00-20:00", "6 09:00-13:00", "6 14:00-24:00" }, periods); // sorted by day and time
        Assert.True(station.GetProperty("amenities").GetProperty("wifi").GetBoolean());

        var saved = await _api.Db(db => db.ChargingStations.SingleAsync(s => s.ID == _stationA));
        Assert.Equal("33.99", saved.Latitude);
        Assert.Equal(ChargingStationStatusEnum.Available, saved.Status); // status is not the partner's to change
        var log = await _api.Db(db => db.AuditLogs.SingleAsync(l => l.Action == "PartnerPortal.UpdateStation"));
        Assert.Equal(_partnerA.ID, log.UserID);
        Assert.Contains("Atlas Agdal", log.ChangesJson); // before
        Assert.Contains("Atlas Agdal Centre", log.ChangesJson); // after
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("coordinates")]
    [InlineData("noPeriods")]
    [InlineData("badTime")]
    [InlineData("reversed")]
    [InlineData("overlap")]
    public async Task Invalid_station_information_is_refused(string problem)
    {
        object body = problem switch
        {
            "name" => Info(name: "A"),
            "description" => Info(description: new string('x', 1001)),
            "coordinates" => Info(latitude: 33.9, longitude: null),
            "noPeriods" => Info(hours: new { twentyFourSeven = false, regular = Array.Empty<object>() }),
            "badTime" => Info(hours: new { twentyFourSeven = false, regular = new[] { new { weekday = 1, periodBegin = "8h", periodEnd = "20:00" } } }),
            "reversed" => Info(hours: new { twentyFourSeven = false, regular = new[] { new { weekday = 1, periodBegin = "20:00", periodEnd = "08:00" } } }),
            _ => Info(hours: new { twentyFourSeven = false, regular = new[] { new { weekday = 2, periodBegin = "08:00", periodEnd = "12:00" }, new { weekday = 2, periodBegin = "11:00", periodEnd = "18:00" } } })
        };

        var response = await _api.Client(_partnerA).PutAsJsonAsync($"/api/partner-portal/stations/{_stationA}", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Atlas Agdal", await _api.Db(db => db.ChargingStations.Where(s => s.ID == _stationA).Select(s => s.Name).SingleAsync()));
    }

    [Fact]
    public async Task A_name_taken_by_another_station_is_refused()
    {
        var response = await _api.Client(_partnerA).PutAsJsonAsync($"/api/partner-portal/stations/{_stationA}", Info(name: "Borne Maarif"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NameTaken", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Nobody_reads_or_edits_another_partner_s_station()
    {
        var a = _api.Client(_partnerA);

        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/partner-portal/stations/{_stationB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/partner-portal/stations/{_stationB}", Info(name: "Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsync($"/api/partner-portal/stations/{_stationB}/photos", Photos(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.Client(_customer).PutAsJsonAsync($"/api/partner-portal/stations/{_stationA}", Info())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Client().GetAsync($"/api/partner-portal/stations/{_stationA}")).StatusCode);
        Assert.Equal("Borne Maarif", await _api.Db(db => db.ChargingStations.Where(s => s.ID == _stationB).Select(s => s.Name).SingleAsync()));
        Assert.Equal(0, await _api.Db(db => db.ChargingStationImages.CountAsync()));
    }

    [Fact]
    public async Task A_partner_adds_and_removes_photos_of_their_station()
    {
        var client = _api.Client(_partnerA);

        var added = await client.PostAsync($"/api/partner-portal/stations/{_stationA}/photos", Photos(Png, Png));
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var photos = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("photos");
        Assert.Equal(2, photos.GetArrayLength());
        Assert.StartsWith("images/charging-stations/", photos[0].GetProperty("url").GetString());
        var photoId = photos[0].GetProperty("id").GetInt32();

        // Partner B cannot remove A's photo, even through their own station id.
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_partnerB).DeleteAsync($"/api/partner-portal/stations/{_stationA}/photos/{photoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.Client(_partnerB).DeleteAsync($"/api/partner-portal/stations/{_stationB}/photos/{photoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/partner-portal/stations/{_stationA}/photos/{photoId}")).StatusCode);

        var station = await client.GetFromJsonAsync<JsonElement>($"/api/partner-portal/stations/{_stationA}");
        Assert.Equal(1, station.GetProperty("photos").GetArrayLength());
        var list = await client.GetFromJsonAsync<JsonElement[]>("/api/partner-portal/stations");
        Assert.StartsWith("images/charging-stations/", list![0].GetProperty("photoUrl").GetString());
        Assert.True(await _api.Db(db => db.AuditLogs.AnyAsync(l => l.Action == "PartnerPortal.AddStationPhotos" && l.UserID == _partnerA.ID)));
        Assert.True(await _api.Db(db => db.AuditLogs.AnyAsync(l => l.Action == "PartnerPortal.RemoveStationPhoto" && l.UserID == _partnerA.ID)));
    }

    [Fact]
    public async Task Only_real_images_are_accepted_as_photos_and_their_number_is_limited()
    {
        var client = _api.Client(_partnerA);

        var script = await client.PostAsync($"/api/partner-portal/stations/{_stationA}/photos", Photos(Encoding.UTF8.GetBytes("<script>alert(1)</script>")));
        var tooMany = await client.PostAsync($"/api/partner-portal/stations/{_stationA}/photos", Photos(Png, Png, Png, Png, Png, Png));
        var none = await client.PostAsync($"/api/partner-portal/stations/{_stationA}/photos", new MultipartFormDataContent { { new StringContent("x"), "note" } });

        Assert.Equal(HttpStatusCode.BadRequest, script.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, none.StatusCode);
        Assert.Equal(0, await _api.Db(db => db.ChargingStationImages.CountAsync()));
    }
}
