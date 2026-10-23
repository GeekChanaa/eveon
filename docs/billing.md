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
- the low-balance stop: on every meter reading (2.0.1 `TransactionEvent Updated`, 1.6 `MeterValues`), when
  `balance − running cost ≤ DefaultLowBalanceThresholdBuffer`, the customer is notified (`LowBalance`) and the
  charger is told to stop (`RequestStopTransaction` / 1.6 `RemoteStopTransaction`);
- the debit when the transaction ends (`TransactionEvent Ended` or 1.6 `StopTransaction`);
- the invoice, the session lists and the revenue statistics, which read the stored amounts.

## Minimum balance

A card needs a positive balance of at least `GlobalConfigurations:DefaultMinimumRequiredBalance` to start: a start
from the app is refused (402), a badge at the charger gets `NoCredit` on `Authorize` and on the transaction start
(1.6: `Invalid`), no session is opened and the customer is notified (`ChargingStartRejectedNoCredit`). Set the
buffer `DefaultLowBalanceThresholdBuffer` to cover the cost of the energy delivered between two meter readings.

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

## Invoices and credit notes

Code: `VoltaXApi/Services/Billing` (`InvoiceService`, `InvoicePdf`, `BillingEvents`, `InvoiceSweeper`).

- **When**: a top-up is invoiced when it is credited (online payment confirmed or manual top-up) and the invoice
  is emailed to the card owner; a session is invoiced once its bill is final (the car left the charger). A session
  whose charger never reports the connector free is invoiced `Invoicing:UnsettledSessionDelayHours` (24) after it
  ended; an idle fee billed after that gets its own invoice (`session:{id}:idle`). Anything a hook missed is
  issued by the sweeper (every 30 minutes, last 7 days) or on first download. Totals of 0 are not invoiced.
- **Numbers**: `EV-{year}-{000001}` for invoices and `AV-{year}-{000001}` for credit notes (prefixes
  `Invoicing:InvoicePrefix` / `CreditNotePrefix`), restarting each business-time-zone year. The next number comes
  from `InvoiceSequences` (concurrency token `LastNumber`) and is committed in the same `SaveChanges` as the invoice
  and its PDF: a failure rolls the number back, two issuers never get the same one, so the series has no gaps.
- **Once**: `Invoices.SourceReference` (`session:12`, `order:5`, `refund:3`) is unique; issuing again returns the
  stored document.
- **Content**: seller with its legal identifiers from the `CompanyInformations` section (name, address, ICE, IF,
  RC, TP, CNSS, phone, e-mail), customer, lines (amounts VAT included), VAT summary (rate, base HT, VAT, TTC),
  payment method. The PDF rendered at issue time is stored in `InvoiceFiles` and served by every download:
  `GET api/my/sessions/{id}/invoice`, `api/my/orders/{id}/invoice`, `api/my/invoices[/{id}/pdf]`,
  `api/ChargingSession/GetChargingSessionInvoice/{id}`, `api/Order/GetOrderInvoice/{id}`, and the register
  `api/Invoice/Search`, `api/Invoice/{id}/Pdf`, `api/Invoice/Journal` (CSV, credit notes negative; permission
  ViewRechargeOrders).
- **Language**: French, English or Arabic (right to left, Noto Sans Arabic, SIL OFL licence in `Assets/Fonts`).

## Refunds

Code: `Services/Billing/RefundService.cs`, `Controllers/RefundController.cs`. Operators with **EditRechargeOrders**
(the permission that credits wallets) refund from the order and session pages; `GET api/Refund/Order/{id}` and
`api/Refund/Session/{id}` (ViewRechargeOrders) tell what is left to refund and how.

| Refund of | Method | Money | Wallet movement |
|---|---|---|---|
| a top-up paid online | `Gateway` (default when the provider can refund) | back to the card through the provider | `Refund`, − |
| any paid top-up | `External` (cash, transfer, the provider's back office) | recorded only | `Refund`, − |
| a billed session | `Wallet` | credited to the wallet | `Refund`, + |

- `POST api/Refund/Order/{id}` / `api/Refund/Session/{id}` `{amount, reason, idempotencyKey, method?}`. The
  dashboard sends a new key per confirmed refund: the same request sent again answers the same refund; the key
  reused for a different refund is refused (409).
- **Never more than paid**: pending and succeeded refunds of an order or session stay within what was paid. The
  check runs again inside the card's optimistic-lock retry, so two concurrent refunds cannot both pass. A top-up
  refund also needs the wallet to still hold the amount (the customer may have spent it).
- **Top-up through the provider**: the refund and the wallet debit are committed first (reserved), then the
  provider is called with the idempotency key `refund-{id}`. If it fails, the refund is `Failed` and the amount is
  given back to the wallet (`refund:{key}:reversal`). Fully refunded orders become `Refunded`.
- Every succeeded refund gets a credit note (`AV-…`) against the invoice of the order or session, and the
  customer gets a notification and an e-mail with the credit note.
- Providers: `Sandbox` refunds at once; `Stripe` refunds the payment intent of the Checkout session
  (`POST /v1/refunds`); `Cmi` and `None` answer "not supported": refund in the CMI back office, then record the
  refund as `External`.

## Partner commission and settlements

Code: `Services/Billing/PartnerSettlementService.cs`, `Controllers/SettlementController.cs` (operators),
`Controllers/PartnerPortal/PartnerStatementsController.cs` (partners).

EVEON collects what drivers pay on partner stations and pays each partner monthly, minus its commission:

- **Terms** per partner (`Partners.CommissionRate`, a fraction, and `Partners.CommissionPerKwh`, MAD excl. VAT),
  set by an operator (`PUT api/Settlement/Commissions/{partnerId}` `{commissionPercent, commissionPerKwh}`,
  EditPartners, audited like every partner change). New partners start at 0.
- **Per line** (one per session, one negative line per session refund):
  `net = round(gross / (1 + session VAT))`, `VAT = gross − net`,
  `commission = round(net × rate) + round(kWh × fee per kWh)`, `commission VAT = round(commission × Vat)`,
  `payout = gross − commission − commission VAT`. The settlement totals are the sums of its lines.
- **Which sessions**: final sessions (car left, or ended more than `Invoicing:UnsettledSessionDelayHours` ago)
  on the partner's stations that ended before the month's end and are in no other settlement
  (`PartnerSettlementLines.SourceKey` is unique). A session reported late goes to the next settlement; nothing
  older than the partner's first settled month is taken. Refunds of settled sessions are deducted from the
  settlement of the month they were made.
- **Status**: `Pending` settlements are recomputed when generated again (current terms, new sessions); an operator
  records the payout (`POST api/Settlement/{id}/Paid` `{reference, paidAt?}`), after which the settlement is
  frozen.
- **When**: on demand (`POST api/Settlement/Generate` `{year, month, partnerId?}`, EditPartners) and by the
  monthly job, which settles the previous month for every partner without a settlement from day
  `Settlements:GenerateOnDay` (2) of the month, business time zone.
- Exports: `GET api/Settlement/{id}/Export?format=csv|pdf&language=fr|en|ar`; partners read theirs under
  `api/partner-portal/statements`.

The model assumes EVEON collects as the partner's agent: the partner declares the VAT of its sales and EVEON
invoices its commission with VAT. Check it with the accountant before the first payout.

## Deploying

Migrations: `MoneyAsDecimal`, `WalletLedger` (adds the ledger and an opening movement for every existing
balance), `SessionBilling`, `LegalInvoices`, `Refunds` and `PartnerSettlements`. Apply them before starting the new build:

```
dotnet ef database update --project VoltaXApi
```

Tests: `dotnet test` (see `tests/VoltaX.Tests/Billing`).
