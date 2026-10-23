# Billing

How a charging session is priced, debited and shown. The code is in `VoltaXApi/Services/CostCalculator.cs`
(formula), `Services/ChargingSessionServices/ChargingSessionService.cs` (session end and idle time) and
`Services/WalletService.cs` (balance changes).

## Prices

Every price includes VAT (TTC), in MAD. A connector carries:

| Field | Meaning |
|---|---|
| `PricePerKWh` | energy delivered |
| `PricePerMinute` | time spent charging (from start to end of the transaction) |
| `FlatFee` | per session, charged only when energy was delivered |
| `PricePerIdleMinute` | time the car stays plugged in after charging ends |
| `CostPerKwh` | the operator's energy cost (margin reporting only, never billed) |

New connectors get the `GlobalConfigurations:Default*` values. Set a price to 0 to switch that component off:
an energy-only tariff has `PricePerMinute = 0`.

When a session starts, the connector's prices and the VAT rate are copied onto the session (`PricePerKWh`,
`PricePerMinute`, `PricePerIdleMinute`, `FlatFee`, `VatRate`). A price change never alters a session that has
already started.

## Formula

```
charge = round(kWh × PricePerKWh) + round(minutes × PricePerMinute) + (kWh > 0 ? round(FlatFee) : 0)
idle   = idleMinutes × 60 > GracePeriod ? round(idleMinutes × PricePerIdleMinute) : 0
total  = charge + idle
```

`round` is to the cent, half away from zero. The total is the sum of the rounded components. Once the idle
time exceeds `GlobalConfigurations:GracePeriod` (seconds), the whole idle time is billed.

The same formula gives:

- the live cost sent to the dashboard / app (SignalR `SessionUpdate`) and to the charger screen (OCPP 2.0.1
  `CostUpdated`), using the session's frozen tariff;
- the low-balance stop: when `balance − running cost ≤ DefaultLowBalanceThresholdBuffer`, the customer is
  notified and `RequestStopTransaction` is sent;
- the debit when the transaction ends (`TransactionEvent Ended` or 1.6 `StopTransaction`);
- the invoice, the session lists and the revenue statistics, which read the stored amounts.

## What a session stores

| Column | Set when |
|---|---|
| `EnergyAmount`, `TimeAmount`, `FlatFeeAmount` | the transaction ends |
| `TotalAmount` | the transaction ends; increased by `IdleAmount` when the car leaves |
| `IdleAmount`, `IdleMinutes` | the connector becomes Available after the session |
| `BalanceBefore`, `BalanceAfter` | the debit (for the invoice) |
| `BilledAt` | the debit |

`TotalAmount = null` marks a session billed before this model (time-only): lists and invoices show those
with the old time-based amount.

## VAT

Amounts are VAT included. The excluding-VAT amount is `round(TTC / (1 + VatRate))` and the VAT is the
remainder, so HT + TVA always equals TTC (`CostCalculator.SplitVat`). The rate is the session's `VatRate`
(the configured `GlobalConfigurations:Vat` when the session started).

## Wallet

The charging card is the wallet. Its balance only changes through `IWalletService`, which writes one
immutable `WalletTransactions` row per change in the same `SaveChanges` as the new balance:

| Type | Reference | Amount |
|---|---|---|
| `TopUp` | `order:{orderId}:topup` | + |
| `SessionCharge` | `session:{sessionId}:charge` | − |
| `IdleFee` | `session:{sessionId}:idle` | − |
| `Refund` | `refund:{...}` | + |
| `Adjustment` | `opening:{...}` or admin | ± |

- **Idempotent**: `Reference` is unique. A retried OCPP message or a replayed payment callback finds the
  existing movement and moves no money.
- **Concurrency**: `Cards.BalanceVersion` is an optimistic concurrency token. A top-up racing a session
  debit makes one of them reload and retry instead of overwriting the other.
- **Atomic session end**: the ended transaction, the session amounts, the new balance and the movement are
  committed in one `SaveChanges`.
- **Invariant**: for every card, the sum of its movements equals its balance. Cards created with a balance
  get an `Adjustment` "Opening balance" movement automatically.

A debit is applied even when it takes the balance below zero (the energy was delivered); a high-criticality
system report is then raised.

## Deploying

Three migrations implement this: `MoneyAsDecimal`, `WalletLedger` (adds the ledger and an opening movement
for every existing balance) and `SessionBilling`. Apply them before starting the new build:

```
dotnet ef database update --project VoltaXApi
```

Tests: `dotnet test` (see `tests/VoltaX.Tests/Billing`).
