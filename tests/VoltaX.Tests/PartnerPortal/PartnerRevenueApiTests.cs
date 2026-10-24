using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.PartnerPortal;

/// <summary>Station revenue shown to partners: what drivers were billed on their stations, nobody else's.</summary>
public class PartnerRevenueApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _partnerUser = null!;
    private int _partnerId, _otherPartnerId, _busyStationId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            var partner = new Partner { Name = "Volt Partner", Description = "", Email = "v@volt.ma", Phone = "0600000003", PartnerIdentificationNumber = "P-V" };
            var other = new Partner { Name = "Someone Else", Description = "", Email = "s@else.ma", Phone = "0600000004", PartnerIdentificationNumber = "P-S" };
            db.Partners.AddRange(partner, other);
            await db.SaveChangesAsync();
            _partnerId = partner.ID;
            _otherPartnerId = other.ID;
            _partnerUser = await Seeds.User(db, "partner@test.ma", role: "Partner", partnerId: partner.ID);
            var busy = await Seeds.Station(db, partner.ID, "Busy");
            var quiet = await Seeds.Station(db, partner.ID, "Quiet");
            _busyStationId = busy.ID;
            var cp = await Seeds.ChargePoint(db, "CP-REV-1", c => c.PricePerKWh = 2.5m, busy.ID);
            await Seeds.ChargePoint(db, "CP-REV-2", c => c.PricePerKWh = 9m, quiet.ID);
            var connector = await db.Connectors.SingleAsync(c => c.ChargePointID == cp.ID);
            var card = Seeds.Card("REV-1", 50m);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            db.ChargingSessions.Add(new ChargingSession
            {
                ConnectorID = connector.ID, CardID = card.ID, StartDate = Seeds.Now.AddHours(-2), EndDate = Seeds.Now.AddHours(-1),
                ChargedKwhs = 12, ChargedMinutes = 60, TotalAmount = 34m, EnergyAmount = 34m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            });
            await db.SaveChangesAsync();
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task The_top_stations_are_ranked_by_what_drivers_were_billed()
    {
        var top = await _api.Client(_partnerUser).GetFromJsonAsync<JsonElement>($"/api/partner/chargingStation/GetPartnerTop10ChargingStations/{_partnerId}");
        var rows = top.EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(_busyStationId, rows[0].GetProperty("chargingStationID").GetInt32());
        Assert.Equal(34m, rows[0].GetProperty("revenue").GetDecimal());
        Assert.Equal(0m, rows[1].GetProperty("revenue").GetDecimal()); // a pricier connector earns nothing without sessions
    }

    [Fact]
    public async Task A_partner_cannot_read_another_partner_s_revenue()
    {
        var response = await _api.Client(_partnerUser).GetAsync($"/api/partner/chargingStation/GetPartnerTop10ChargingStations/{_otherPartnerId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_per_station_revenue_endpoints_based_on_connector_prices_are_gone()
    {
        var response = await _api.Client(_partnerUser).GetAsync($"/api/ChargingStation/GetPartnerChargingStationRevenue/{_partnerId}?chargingStationID={_busyStationId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
