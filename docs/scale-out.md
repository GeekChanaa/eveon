# Running several API instances (scale-out)

The API runs as one instance by default, with all state in memory. Setting `ScaleOut:Redis:ConnectionString`
lets N instances run behind a load balancer and share:

| Concern | Single instance (no Redis) | Redis configured |
| --- | --- | --- |
| Charger ownership (which instance holds the WebSocket) | in-memory registry | `eveon:ocpp:owner:{chargePointId}` (JSON, TTL) |
| CSMS commands (`IOcppCommandSender`) | local socket | local socket, else forwarded to the owning instance |
| Charger status / protocol version | local connections | local, else the registry |
| SignalR events (dashboards) | in-process | Redis backplane, channel prefix `eveon` |
| SignalR revocation (`HubConnections`) | local | local + broadcast on `eveon:hub:revoke` |
| 2FA attempts / used challenges, Google link tickets | `IDistributedCache` (memory) | Redis `eveon:sec:*` (`GETDEL`, `INCR`, `SET NX`) |
| `IDistributedCache` | `AddDistributedMemoryCache` | `AddStackExchangeRedisCache` (`eveon:cache:` prefix) |

Code: `VoltaXApi/ScaleOut/`. Wiring: `ServiceRegistration.ConfigureScaleOut`.

## Configuration

| Key | Default | Notes |
| --- | --- | --- |
| `ScaleOut__Redis__ConnectionString` | empty | StackExchange.Redis syntax, e.g. `redis:6379,password=...,ssl=true`. Empty = single-instance mode |
| `ScaleOut__InstanceId` | `<machine>-<pid>-<random>` | must be unique per running process; leave empty unless you need stable ids in logs |
| `ScaleOut__HeartbeatSeconds` | 30 | how often an instance refreshes the ownership of its chargers |
| `ScaleOut__OwnershipTtlSeconds` | 90 | ownership expires when not refreshed (crashed instance); at least 2 × heartbeat |
| `Ocpp__CommandTimeoutSeconds` | 30 | unchanged; also applies to forwarded commands |

Data Protection keys are already stored in the database (`PersistKeysToDbContext`), so tokens and 2FA
challenges issued by one instance are readable by the others. JWTs are stateless.

## How it works

**Ownership.** When a charger connects, its instance claims `eveon:ocpp:owner:{id}` =
`{instanceId, protocolVersion, connectedAt, lastSeen}` with a 90 s TTL. If another instance owned it, that
instance receives a `close-stale` message (`eveon:ocpp:close-stale:{instanceId}`) and closes its old socket.
Every 30 s each instance refreshes its entries; a socket whose entry now names another instance is closed
(covers a lost close-stale message). On disconnect the entry is removed only if this instance still owns it;
an instance that lost the charger to another one does not mark it offline in the database.

**Commands.** `SendRequestAsync` uses the local socket when the charger is connected here. Otherwise it looks
up the owner and publishes `{requestId, chargePointId, action, payloadJson, timeoutMs, replyChannel}` on
`eveon:ocpp:cmd:{ownerInstanceId}`. The owner runs it through its local sender (same one-CALL-at-a-time rule,
same timeout) and answers on `eveon:ocpp:reply:{callerInstanceId}` with
`{status: ok|callerror|timeout|notconnected, payloadJson, errorCode, errorDescription}`. The caller maps it back
to the same result/exceptions as a local call (`OcppCallErrorException`, `TimeoutException`,
`WebSocketNotFoundException`). The caller waits the command timeout + 2 s for the reply.

**Health.** `GET /health/live` (process up, no checks) and `GET /health/ready` (database; plus Redis when
configured — Redis down reports `Degraded`, HTTP 200, so the instance stays in rotation for its own chargers).

## Running two instances

`VoltaXApi/deploy/docker-compose.scaleout.yml` starts Redis, two API replicas and Caddy
(`VoltaXApi/deploy/Caddyfile.scaleout`):

```bash
cd VoltaXApi/deploy
API_ENV_FILE=/etc/eveon/api.env docker compose -f docker-compose.scaleout.yml up -d --build
docker compose -f docker-compose.scaleout.yml up -d --scale api=3   # more replicas
```

Load balancing:

* **Chargers (`/ocpp/...`) and REST: no affinity** (`least_conn`). A charger that reconnects may land on any
  instance; that instance takes over its ownership and the old one closes the stale socket.
* **SignalR hubs: cookie affinity.** SignalR negotiates over HTTP and then opens the transport with a
  connection id only the negotiating instance knows, so hub requests must reach the same instance. Events still
  reach every dashboard through the Redis backplane. (Clients that use `skipNegotiation: true` with the WebSocket
  transport would not need affinity.)

## Failure modes

| Failure | Effect |
| --- | --- |
| Redis down | Each instance keeps serving the chargers connected to it (local commands, inbound messages, DB). Commands for chargers on another instance fail at once with `ScaleOutUnavailableException` (a `WebSocketNotFoundException`, answered 409 "not connected" by `OcppCommandResult` today; map it to 503 there). SignalR events only reach clients on the same instance. 2FA verification and Google link tickets fail (500) until Redis is back. `/health/ready` reports Degraded. Subscriptions are retried every 5 s and restored by the Redis client on reconnect |
| Instance crash | Its chargers reconnect to other instances (taking over ownership at once). Entries of chargers that do not reconnect expire after the TTL (90 s); until then commands to them are forwarded to a dead channel and fail at once as "not connected" (0 subscribers) |
| Owner does not answer a forwarded command | Caller throws `TimeoutException` after the command timeout + 2 s |
| Charger flaps between instances | Each connect claims ownership; `connectedAt` stops a late close-stale from killing the newer socket; the heartbeat corrects any leftover |

## Known limits

* Rate limiting is per instance (the effective limit is N × the configured value).
* Background services (card expiry warnings, data retention, GDPR workers, refresh token cleanup) run on every
  instance; they must be idempotent or moved behind a leader lock before running many replicas.
* `ChargePointConnectivityNotifier`, `ConnectorReportBuffer` and the OCPI caches are per instance.
  The connectivity notifier only checks local sockets, so a charger that moves to another instance within the
  2-minute grace period can still produce a "disconnected/offline" notification from the old instance.
* `ChargePointController` (IsOnline), `ChargePointRealTimeController` and `ProvisioningService.IsOnline` read
  `WebSocketManagerService` (local sockets only) and show chargers on other instances as offline; they should use
  `ChargePointStatusManagerService.ChargePointExists`, which checks the registry.
* A forwarded command is not cancelled on the owner when the caller cancels; the owner finishes it and the
  reply is dropped.
* Connector live state (`ChargePointStatus.OnlineConnectors`) stays on the owning instance; remote reads get id
  and protocol only.
