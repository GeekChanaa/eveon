# OCPP in EVEON / VoltaX

The CSMS lives in `VoltaXApi/OCPP`. Chargers connect over WebSocket to `/ocpp/{chargePointId}` and negotiate a
sub-protocol at the handshake.

## Supported versions

| Sub-protocol | Status | Notes |
|---|---|---|
| `ocpp2.0.1` | Full CSMS (preferred when a charger offers both) | Handlers in `OCPP/OCPP.Handlers`, messages in `OCPP/OCPP.Messages` |
| `ocpp1.6` (1.6J) | Core, Firmware Management, Local Auth List, Reservation, Remote Trigger | Code in `OCPP/Ocpp16` |

A version is offered at the handshake only when at least one inbound handler is registered for it
(`OcppInboundHandlerRegistry` / `WebSocketSubProtocolMatcher`), most preferred first: 2.0.1, then 1.6.

Both versions share the same business rules through version-neutral services:

| Concern | Service |
|---|---|
| Boot acceptance (unknown = Rejected, never provisioned = Pending, provisioned = Accepted) | `OCPP.Services/ChargePointBootService` (+ `ChargePointRegistration`) |
| Token / idTag authorization (card active/blocked/expired, OCPI tokens through `IExternalTokenAuthorizer`) | `OCPP.Services/IdTokenAuthorizationService` |
| Sessions, billing, customer notifications, SignalR updates, low-balance stop | `Services/TransactionService` fed with `TransactionEventData` |
| Roaming (OCPI) sessions | `IRoamingTransactionObserver` (1.6 through `Ocpi/Services/RoamingTransactionEvent`) |
| Connector status model | `Services/ConnectorStatusService` |
| Meter normalization (kWh / kW / SoC) | `OCPP.Helpers/MeterValueNormalizer` (1.6 samples are converted by `Ocpp16Mapping.ToMeterValues`) |
| Reservations (both versions) | `OCPP.Services/ReservationService`, `ReservationExpiryService` (sweeps expired ones every minute) |

## OCPP 1.6 model mapping

- **Connectors**: 1.6 connector `N` is our EVSE `N` with one connector (`Connector.EvseID = N`, `ConnectorID = 1`).
  A connector a 1.6 charger reports in StatusNotification that is not configured yet is created with the default
  pricing (GlobalConfigurations), like the 2.0.1 inventory does. Connector `0` is the charger itself (logged only).
- **Status**: `Available` → Available, `Preparing`/`Charging`/`SuspendedEV`/`SuspendedEVSE`/`Finishing` → Occupied,
  `Reserved` → Reserved, `Unavailable` → Unavailable, `Faulted` → Faulted. `errorCode` other than `NoError` is logged
  and kept in the MessageLog.
- **Transactions**: StartTransaction always gets an integer `transactionId` = `Ocpp16Transactions.Id` (even when
  refused, as the charger needs one). The matching `Transactions.Uid` (and OCPI `TransactionUid`) is `ocpp16-{Id}`.
  `RequestStopTransaction` accepts either form for a 1.6 charger.
- **Meter values**: 1.6 has no multiplier; the unit defaults to `Wh` (energy) / `W` (power); `kWh`/`kW` are converted.
  `SignedData` samples and unknown measurands are ignored. `meterStart`/`meterStop` are Wh.
- **Authorization statuses**: 2.0.1-only results (NoCredit, NotAtThisLocation, ...) are sent as `Invalid` to 1.6 chargers.
- **Billing**: StopTransaction bills exactly like a 2.0.1 `Ended` event (on `meterStop`; `transactionData` only feeds
  the live view). A transaction that was refused at start is never billed.

## Message coverage

Inbound = charger → CSMS, outbound = CSMS → charger. "routed" means the existing 2.0.1-shaped endpoint/service
detects a 1.6 charger (`IOcppCommandSender.GetProtocolVersion`) and sends the 1.6 message instead.

### Inbound

| Message | 2.0.1 | 1.6 |
|---|---|---|
| BootNotification | ✓ | ✓ |
| Heartbeat | ✓ | ✓ |
| StatusNotification | ✓ | ✓ |
| Authorize | ✓ | ✓ |
| TransactionEvent | ✓ (Started without EVSE supported) | – |
| StartTransaction / StopTransaction | – | ✓ |
| MeterValues | ✓ | ✓ |
| DataTransfer | ✓ | ✓ |
| FirmwareStatusNotification | ✓ | ✓ |
| DiagnosticsStatusNotification | – | ✓ |
| LogStatusNotification | ✓ | – |
| ReservationStatusUpdate | ✓ | – (1.6 has none) |
| SecurityEventNotification, SignCertificate, GetCertificateStatus, Get15118EVCertificate | ✓ | – |
| NotifyReport, NotifyEvent, NotifyMonitoringReport, NotifyCustomerInformation, NotifyDisplayMessages | ✓ | – |
| NotifyChargingLimit, ClearedChargingLimit, NotifyEVChargingNeeds, NotifyEVChargingSchedule, ReportChargingProfiles | ✓ | – |

The authoritative list is `ServiceRegistration.ConfigureOCPPHandlers` and `Ocpp16Registration`.

### Outbound

| Endpoint (`/ocpp/...`) | 2.0.1 message | 1.6 message |
|---|---|---|
| `EVDriver/RequestStartTransaction(Mobile)` | RequestStartTransaction | RemoteStartTransaction (routed; idTag resolved server-side for mobile) |
| `EVDriver/RequestStopTransaction(Mobile)` | RequestStopTransaction | RemoteStopTransaction (routed, integer id) |
| `EVDriver/UnlockConnector` | UnlockConnector | UnlockConnector (routed; connectorId = evseId) |
| `EVDriver/ReserveNow`, `EVDriver/CancelReservation` | ReserveNow / CancelReservation | same (routed); persisted in `Reservations` |
| `EVDriver/ClearCache`, `SendLocalList`, `GetLocalListVersion` | same | same (routed) |
| `Configuration/Reset` | Reset | Reset (routed; Immediate → Hard, OnIdle → Soft; per-EVSE reset → 400) |
| `Configuration/ChangeAvailability` | ChangeAvailability | ChangeAvailability (routed) |
| `Configuration/TriggerMessage` | TriggerMessage | TriggerMessage (routed; LogStatusNotification → DiagnosticsStatusNotification) |
| `Configuration/UpdateFirmware` | UpdateFirmware | UpdateFirmware (routed; location/retrieveDate; signed firmware → 400) |
| `Configuration/RefreshConnectors` | GetBaseReport (SummaryInventory) | TriggerMessage StatusNotification |
| `Configuration/ChangeConfiguration` `{key,value}` | – (400, use SetVariables) | ChangeConfiguration |
| `Configuration/GetConfiguration` `{keys[]}` | – (400, use GetVariables) | GetConfiguration |
| `Configuration/GetDiagnostics` `{location?,startTime?,stopTime?,retries?,retryInterval?}` | – (400, use GetLog) | GetDiagnostics (without `location` a one-time upload URL from `ILogUploadUrlFactory` is used; DiagnosticsStatusNotification updates that ticket) |
| `Configuration/ProtocolVersion` (GET) | `{protocolVersion, isOnline}` of the live connection | |
| `Configuration/SetNetworkProfile`, display messages, Publish/UnpublishFirmware, monitoring, device model, certificates, ... | ✓ | 400 unless the owning service implements a 1.6 variant |
| Smart charging (`SetChargingProfile`, `ClearChargingProfile`, `GetCompositeSchedule`) | ✓ | see `SmartChargingService` |

A command that does not exist in the charger's version answers **HTTP 400**
`{ message, error, status: "NotSupportedByProtocol" }` (`OcppProtocolNotSupportedException`); nothing is sent.
Other outcomes are unchanged: 200 with the charger's answer, 409 not connected, 504 timeout, 502 CALLERROR.

## Reservations

`ReserveNow` stores a `Reservations` row (status Active) before sending; the charger reservation id is the
caller's `id` when positive (dashboard, OCPI), otherwise the row id. It becomes Rejected when the charger refuses
or does not answer, Used when a transaction starts with it (1.6 `StartTransaction.reservationId`, 2.0.1
`TransactionEvent.reservationId`), Cancelled after an accepted CancelReservation or a 2.0.1
`ReservationStatusUpdate(Removed)`, and Expired from `ReservationStatusUpdate(Expired)` or the sweeper.

## OCPP 2.0.1: authorization before plug-in

A `TransactionEvent(Started)` without EVSE is accepted: the token is checked like any start (same answer and
customer notifications) and the transaction is kept in `OcppPendingTransactions`. The first later event carrying the
EVSE starts the session with the Started event's timestamp and meter, then is applied itself. Nothing is billed
before a connector is known; a transaction that ends without ever reporting an EVSE is dropped.

## Adding an inbound handler

1. Implement `IOCPPRequestHandler` (for 1.6, derive from `Ocpp16HandlerBase<TRequest, TResponse>`: it deserializes
   with `OCPPMessageFactory.DefaultSettings`, answers FormationViolation for unreadable payloads, serializes the
   response and writes the MessageLog).
2. Register it: `services.AddOcppInboundHandler<MyHandler>(OcppProtocols.Ocpp16, "Action")` (1.6 handlers in
   `Ocpp16Registration.AddOcpp16`, 2.0.1 ones in `ServiceRegistration.ConfigureOCPPHandlers`). Each CALL runs in a
   fresh DI scope, so scoped services/DbContext are safe.
3. Return `null` for success or an `ErrorCodes` value for a CALLERROR (mapped to the version's wire name).
   Unhandled exceptions become `InternalError` and are logged.
4. Never await a CSMS → charger command inside a handler; use `OcppBackgroundCommand.Run`.

Outgoing commands use `IOcppCommandSender.SendRequestAsync<TReq,TRes>(chargePointId, action, request)`; route by
`GetProtocolVersion(chargePointId)` (or `Ocpp16CommandService.IsOcpp16 / Require16 / Require201`).

## Security profile notes

- Charger authentication is version independent (`WebSocketRequestsHandler` / `OCPPAuthenticationService`):
  profile 1 = HTTP Basic (plain `ws://` only with `Ocpp:AllowInsecureProfile1`), profile 2 = TLS + Basic,
  profile 3 = TLS + client certificate from the charger CA. Passwords are stored hashed.
- 1.6 chargers use the same profiles (OCPP 1.6 Security Whitepaper). The 1.6 security extension messages
  (SignCertificate, CertificateSigned, InstallCertificate, SecurityEventNotification, SignedUpdateFirmware,
  GetLog) are not implemented for 1.6 yet; signed firmware is refused with 400 for 1.6 chargers.
- 1.6 chargers boot Pending until provisioned or skipped on the provisioning page; while Pending they may
  not start transactions (Authorize and StartTransaction answer Invalid).
