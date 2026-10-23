# OCPP message samples

Hand-checked OCPP-J frames for trying the CSMS by hand (a WebSocket client connected to
`ws://localhost:<port>/ocpp/<chargePointId>` with sub-protocol `ocpp2.0.1` or `ocpp1.6`, see
[../ocpp.md](../ocpp.md)). Each file maps a name to one frame: `[2, messageId, action, payload]` for a
call, `[3, messageId, payload]` for a result.

| File | Content |
| --- | --- |
| `ocpp2.0.1-charger-to-csms.json` | boot, security event, status, authorize, a full transaction (Started, Updated, Ended) |
| `ocpp2.0.1-csms-to-charger.json` | remote start / stop as the CSMS sends them, a boot result |
| `ocpp1.6-csms-to-charger.json` | 1.6 remote start / stop and unlock |

Identifiers are placeholders (`SAMPLE-TOKEN-0001`, `SAMPLE-0001`); a token must exist (and be active) for
the CSMS to accept it. Never paste real card numbers, serial numbers or logs here. For automated runs use
the simulator and its end-to-end script instead.
