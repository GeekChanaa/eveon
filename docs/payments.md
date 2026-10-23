# Payments

Customers top up their wallet (their charging card) online and keep saved cards at the payment provider. The
API never sees a card number: the customer types it on the provider's page. A wallet is credited **only** when
the provider confirms the payment in a signed server-to-server notification, through the wallet ledger
(`order:{id}:topup`, see [billing.md](billing.md)), so a notification delivered twice credits once.

Code: `VoltaXApi/Services/Payments` (gateways, `PaymentService`), `Controllers/SelfService/PaymentsController.cs`
(customer API), `Controllers/PaymentCallbacksController.cs` (provider callbacks, return URLs, sandbox pages).

## Providers

`Payments:Provider` selects one:

| Provider | Use | Saved cards |
|---|---|---|
| `None` (default) | online top-up off; the API answers 503 "not available" | no |
| `Sandbox` | development / test environments: a test page approves, declines or cancels; **no money moves** | yes (Visa •••• 4242) |
| `Cmi` | Morocco, CMI 3D Secure hosted page | no |
| `Stripe` | Stripe Checkout | yes |

`Sandbox` is refused at startup in Production unless `Payments:Sandbox:AllowInProduction=true` (only for a
test environment: anyone with an account can credit their wallet).

## Settings

| Key | Example | Notes |
|---|---|---|
| `Payments__Provider` | `Cmi` | |
| `Payments__PublicApiUrl` | `https://api-v2.eveon.ma` | providers call back here |
| `Payments__SpaUrl` | `https://dashboard-v2.eveon.ma` | where the customer lands after paying |
| `Payments__MinTopUp`, `Payments__MaxTopUp` | `20`, `5000` | MAD |
| `Payments__Sandbox__Secret` | 32+ random chars | signs the sandbox forms |
| `Payments__Cmi__ClientId` | merchant id | from CMI |
| `Payments__Cmi__StoreKey` | secret | from CMI, **secret** |
| `Payments__Cmi__GatewayUrl` | `https://testpayment.cmi.co.ma/fim/est3Dgate` | production URL from CMI |
| `Payments__Stripe__SecretKey` | `sk_live_...` | **secret** |
| `Payments__Stripe__WebhookSecret` | `whsec_...` | **secret** |

Local development with the sandbox:

```
cd VoltaXApi
dotnet user-secrets set "Payments:Provider" "Sandbox"
dotnet user-secrets set "Payments:Sandbox:Secret" "<any long random string>"
```

## Flow

1. The dashboard calls `POST /api/payments/top-ups {amount, cardId?}`. The API creates a `Pending` order and
   answers how to reach the provider: `{ orderId, checkout: { method: "GET" | "POST", url, fields } }`. For
   `POST` (CMI) the dashboard auto-submits a form with `fields` to `url`.
2. The customer pays on the provider's page.
3. The provider notifies `POST /api/payments/webhooks/{Provider}` (CMI `callbackUrl`, Stripe webhook). The
   signature is verified (CMI: SHA-512 hash v3 with the store key; Stripe: `Stripe-Signature`, 5 minutes
   tolerance), the amount must equal the order amount, the event is recorded in `PaymentEvents` (unique per
   provider and event id), the order becomes `Completed` and the card is credited.
4. The browser comes back through `/api/payments/return/{Provider}`, which only redirects to
   `/my-dashboard/wallet?topup={orderId}`; the dashboard polls `GET /api/payments/top-ups/{orderId}`.

Declined / canceled payments mark the order `Failed` / `Canceled`; nothing is credited.

Saved cards: `POST /api/payments/methods/setup` opens the provider's card page; the provider's notification
saves the card (brand, last 4 digits, expiry and the provider's token, never shown). `GET /api/payments/methods`,
`POST /api/payments/methods/{id}/default`, `DELETE /api/payments/methods/{id}` manage them. With Stripe, saved
cards are offered on the next checkout.

Operators credit cash or bank transfers with `POST /api/Order/CreateRechargeOrder {cardID, amount, note}`
(permission EditRechargeOrders): the order is `Manual`, the operator and the reason are on record.

## Registering the callbacks

- **CMI**: the callback URL is sent with every payment (`callbackUrl` field); CMI must allow it for the
  merchant account. Answer to an approved payment: `ACTION=POSTAUTH` (captures the pre-authorization),
  otherwise `APPROVED` / `FAILURE`. Validate the whole flow on CMI's test platform with the merchant's
  integration guide before going live.
- **Stripe**: Dashboard → Developers → Webhooks → endpoint `https://<api>/api/payments/webhooks/Stripe` with the
  events `checkout.session.completed`, `checkout.session.async_payment_succeeded`,
  `checkout.session.async_payment_failed`, `checkout.session.expired`; copy its signing secret to
  `Payments__Stripe__WebhookSecret`.

Tests: `tests/VoltaX.Tests/Payments`.
