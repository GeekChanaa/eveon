using Microsoft.Extensions.Logging.Abstractions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;

namespace VoltaX.Tests.Ocpp;

/// <summary>CostUpdated: the running cost shown on a 2.0.1 charger is the amount the session will be billed.</summary>
public class CostUpdatedTests
{
    private static readonly Tariff Tariff = Tariff.Of(new Connector { PricePerKWh = 3.2m, PricePerMinute = 0.45m, PricePerIdleMinute = 0.1m, FlatFee = 1m }, 0.2m);

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(12.5, 30.0)]
    [InlineData(40.0, 10.0)]
    [InlineData(1.0, 300.0)]
    public void The_running_cost_equals_the_billed_amount(double kwh, double minutes)
    {
        // Flat fee (when energy was delivered) + energy + time, each rounded to the cent.
        var expected = Math.Round((decimal)kwh * 3.2m, 2, MidpointRounding.AwayFromZero) + Math.Round((decimal)minutes * 0.45m, 2, MidpointRounding.AwayFromZero) + (kwh > 0 ? 1m : 0m);

        Assert.Equal(expected, CostCalculator.Instance.RunningCost(Tariff, kwh, minutes));
        Assert.Equal(expected, CostCalculator.Instance.Charge(Tariff, kwh, minutes).Total);
    }

    [Fact]
    public void An_active_transaction_is_costed_with_the_billing_formula_and_never_negative()
    {
        var start = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
        var active = new CostUpdatedHostedService.ActiveTransaction("CP-1", 1, "tx-1", start, 1000, 1012.5, Tariff);

        Assert.Equal(CostCalculator.Instance.RunningCost(Tariff, 12.5, 30), CostUpdatedHostedService.RunningCost(CostCalculator.Instance, active, start.AddMinutes(30)));
        // No meter value yet and a clock behind the start time.
        Assert.Equal(0, CostUpdatedHostedService.RunningCost(CostCalculator.Instance, active with { MeterStop = null }, start.AddMinutes(-5)));
    }

    [Fact]
    public void The_total_cost_is_rounded_to_the_cent_and_never_negative()
    {
        Assert.Equal(12.35m, CostUpdatedSender.RoundCost(12.345m));
        Assert.Equal(0m, CostUpdatedSender.RoundCost(-1));
    }

    private sealed class Sender : IOcppCommandSender
    {
        public Dictionary<string, string> Protocols { get; } = new() { ["CP-1"] = OcppProtocols.Ocpp201 };
        public Dictionary<string, string> Reject { get; } = new();
        public List<(string ChargePointId, string Action, decimal TotalCost, string TransactionId)> Sent { get; } = new();
        public int Calls { get; private set; }

        public string? GetProtocolVersion(string chargePointId) => Protocols.TryGetValue(chargePointId, out var p) ? p : null;

        public Task<TResponse> SendRequestAsync<TRequest, TResponse>(string chargePointId, string action, TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Reject.TryGetValue(chargePointId, out var code))
                throw new OcppCallErrorException(code, null);
            var cost = (CostUpdatedRequest)(object)request!;
            Sent.Add((chargePointId, action, cost.TotalCost, cost.TransactionId));
            return Task.FromResult((TResponse)(object)new CostUpdatedResponse());
        }
    }

    [Fact]
    public async Task CostUpdated_goes_to_connected_2_0_1_chargers_only()
    {
        var fake = new Sender { Protocols = { ["CP-16"] = OcppProtocols.Ocpp16 } };
        var sender = new CostUpdatedSender(fake, NullLogger<CostUpdatedSender>.Instance);

        Assert.True(await sender.SendAsync("CP-1", "tx-1", 4.567m));
        Assert.Equal(("CP-1", "CostUpdated", 4.57m, "tx-1"), Assert.Single(fake.Sent));
        Assert.False(await sender.SendAsync("CP-16", "tx-2", 1));  // 1.6 has no CostUpdated
        Assert.False(await sender.SendAsync("CP-OFF", "tx-3", 1)); // offline
        Assert.Single(fake.Sent);
    }

    [Fact]
    public async Task A_charger_answering_NotImplemented_is_not_asked_again()
    {
        var fake = new Sender { Protocols = { ["CP-NO"] = OcppProtocols.Ocpp201 }, Reject = { ["CP-NO"] = "NotImplemented" } };
        var sender = new CostUpdatedSender(fake, NullLogger<CostUpdatedSender>.Instance);

        Assert.False(await sender.SendAsync("CP-NO", "tx-4", 1));
        Assert.True(sender.IsSuppressed("CP-NO"));
        var calls = fake.Calls;
        Assert.False(await sender.SendAsync("CP-NO", "tx-4", 2));
        Assert.Equal(calls, fake.Calls);
    }

    [Fact]
    public async Task Other_call_errors_surface_and_do_not_suppress_the_charger()
    {
        var fake = new Sender { Reject = { ["CP-1"] = "InternalError" } };
        var sender = new CostUpdatedSender(fake, NullLogger<CostUpdatedSender>.Instance);

        await Assert.ThrowsAsync<OcppCallErrorException>(() => sender.SendAsync("CP-1", "tx-1", 1));
        Assert.False(sender.IsSuppressed("CP-1"));
    }
}
