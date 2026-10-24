using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.Models;

namespace VoltaX.Tests.Security;

/// <summary>
/// Cross-tenant access with data that really belongs to another tenant: customer A must not reach customer B's
/// sessions, orders, cards or notification settings through the dashboard endpoints, and partner A must not reach
/// or change partner B's connector prices. Ids here exist and belong to someone else, not to nobody.
/// </summary>
public class CrossTenantIdorTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private User _a = null!, _b = null!, _pa = null!, _pb = null!;
    private int _bSessionId, _bOrderId, _bConnectorId, _aConnectorId;

    public async Task InitializeAsync()
    {
        await _api.Seed(async db =>
        {
            _a = await Seeds.User(db, "a@test.ma");
            _b = await Seeds.User(db, "b@test.ma");
            var pA = new Partner { Name = "A", Description = "", Email = "pa@test.ma", Phone = "0600000001", PartnerIdentificationNumber = "P-A" };
            var pB = new Partner { Name = "B", Description = "", Email = "pb@test.ma", Phone = "0600000002", PartnerIdentificationNumber = "P-B" };
            db.Partners.AddRange(pA, pB);
            await db.SaveChangesAsync();
            _pa = await Seeds.User(db, "partner-a@test.ma", role: "Partner", partnerId: pA.ID);
            _pb = await Seeds.User(db, "partner-b@test.ma", role: "Partner", partnerId: pB.ID);

            var stationA = await Seeds.Station(db, pA.ID, "Station A");
            var stationB = await Seeds.Station(db, pB.ID, "Station B");
            var cpA = await Seeds.ChargePoint(db, "CP-A", c => c.PricePerKWh = 2m, stationA.ID);
            var cpB = await Seeds.ChargePoint(db, "CP-B", c => c.PricePerKWh = 2m, stationB.ID);
            _aConnectorId = (await db.Connectors.SingleAsync(c => c.ChargePointID == cpA.ID)).ID;
            _bConnectorId = (await db.Connectors.SingleAsync(c => c.ChargePointID == cpB.ID)).ID;

            var cardB = Seeds.Card("B-1", 50m, _b.ID);
            db.Cards.Add(cardB);
            await db.SaveChangesAsync();
            var sessionB = new ChargingSession
            {
                ConnectorID = _bConnectorId, UserID = _b.ID, CardID = cardB.ID, StartDate = Seeds.Now.AddHours(-1), EndDate = Seeds.Now,
                ChargedKwhs = 5, TotalAmount = 10m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed
            };
            db.ChargingSessions.Add(sessionB);
            var orderB = new Order { CardID = cardB.ID, Amount = 100m, Status = RechargeOrderStatus.Completed, Provider = "Sandbox", PaidAt = Seeds.Now, RechargeDate = Seeds.Now };
            db.Orders.Add(orderB);
            await db.SaveChangesAsync();
            _bSessionId = sessionB.ID;
            _bOrderId = orderB.ID;
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static bool Denied(HttpStatusCode s) => s is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized;

    [Fact]
    public async Task Customer_A_cannot_read_customer_B_s_sessions_orders_or_cards()
    {
        var a = _api.Client(_a);

        async Task<HttpStatusCode> S(string url) => (await a.GetAsync(url)).StatusCode;

        Assert.True(Denied(await S($"/api/ChargingSession/GetChargingSessionInformations/{_bSessionId}")));
        Assert.True(Denied(await S($"/api/ChargingSession/GetChargingSessionInvoice/{_bSessionId}")));
        Assert.True(Denied(await S($"/api/ChargingSession/GetUserChargingSessions/{_b.ID}")));
        Assert.True(Denied(await S($"/api/Order/GetOrder/{_bOrderId}")));
        Assert.True(Denied(await S($"/api/Order/GetInvoiceInfo/{_bOrderId}")));
        Assert.True(Denied(await S($"/api/Order/GetOrderInvoice/{_bOrderId}")));
        Assert.True(Denied(await S($"/api/Order/GetUserOrders/{_b.ID}")));
        Assert.True(Denied(await S($"/api/Card/GetUserRechargeCards/{_b.ID}")));
        Assert.True(Denied(await S($"/api/NotificationSetting/UserNotificationSettings/{_b.ID}")));
    }

    [Fact]
    public async Task Customer_A_reads_their_own_session()
    {
        int myId = 0;
        await _api.Seed(async db =>
        {
            var card = Seeds.Card("A-1", 20m, _a.ID);
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            var mine = new ChargingSession { ConnectorID = _aConnectorId, UserID = _a.ID, CardID = card.ID, StartDate = Seeds.Now.AddHours(-1), EndDate = Seeds.Now,
                ChargedKwhs = 3, TotalAmount = 6m, ChargingSessionStatus = ChargingSessionStatusEnum.Completed };
            db.ChargingSessions.Add(mine);
            await db.SaveChangesAsync();
            myId = mine.ID;
        });

        var response = await _api.Client(_a).GetAsync($"/api/ChargingSession/GetChargingSessionInformations/{myId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_save_a_notification_setting_for_another_user()
    {
        var before = await _api.Db(db => db.NotificationSettings.CountAsync(s => s.UserID == _b.ID));

        var response = await _api.Client(_a).PostAsJsonAsync("/api/NotificationSetting/SaveUserNotificationSetting",
            new { userID = _b.ID, notificationTypeID = (int?)null, active = true, email = true });

        Assert.True(Denied(response.StatusCode), response.StatusCode.ToString());
        Assert.Equal(before, await _api.Db(db => db.NotificationSettings.CountAsync(s => s.UserID == _b.ID)));
    }

    [Fact]
    public async Task Partner_A_cannot_read_or_price_partner_B_s_connector()
    {
        var a = _api.Client(_pa);

        var read = await a.GetAsync($"/api/partner-portal/charge-points/{await ChargePointOfConnector(_bConnectorId)}");
        var price = await a.PutAsJsonAsync($"/api/partner-portal/connectors/{_bConnectorId}/pricing",
            new { pricePerKWh = 0.01m, pricePerMinute = 0m, pricePerIdleMinute = 0m, flatFee = 0m });

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, price.StatusCode);
        Assert.Equal(2m, await _api.Db(db => db.Connectors.Where(c => c.ID == _bConnectorId).Select(c => c.PricePerKWh).SingleAsync()));
    }

    [Fact]
    public async Task Partner_A_can_price_their_own_connector()
    {
        var response = await _api.Client(_pa).PutAsJsonAsync($"/api/partner-portal/connectors/{_aConnectorId}/pricing",
            new { pricePerKWh = 3.5m, pricePerMinute = 0m, pricePerIdleMinute = 0m, flatFee = 0m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3.5m, await _api.Db(db => db.Connectors.Where(c => c.ID == _aConnectorId).Select(c => c.PricePerKWh).SingleAsync()));
    }

    private Task<int> ChargePointOfConnector(int connectorId) =>
        _api.Db(db => db.Connectors.Where(c => c.ID == connectorId).Select(c => c.ChargePointID ?? 0).SingleAsync());
}
