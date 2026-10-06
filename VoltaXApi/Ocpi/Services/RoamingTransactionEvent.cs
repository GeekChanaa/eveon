using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaXApi.Ocpi.Services
{
    /// <summary>
    /// Version-neutral transaction event for <see cref="IRoamingTransactionObserver"/> (OCPP 1.6 Start/Stop/MeterValues).
    /// <see cref="EvseId"/> is the 1.6 connectorId (our EVSE), <see cref="MeterWh"/> the energy register in Wh.
    /// </summary>
    public sealed record RoamingTransactionEvent(
        TransactionEventEnumType EventType,
        string TransactionUid,
        string? IdTag,
        int? EvseId,
        double? MeterWh,
        DateTime Timestamp)
    {
        public TransactionEventRequest ToTransactionEventRequest()
        {
            var context = EventType switch
            {
                TransactionEventEnumType.Started => ReadingContextEnumType.Transaction_Begin,
                TransactionEventEnumType.Ended => ReadingContextEnumType.Transaction_End,
                _ => ReadingContextEnumType.Sample_Periodic
            };
            var timestamp = DateTime.SpecifyKind(Timestamp, DateTimeKind.Utc);
            return new TransactionEventRequest
            {
                EventType = EventType,
                TriggerReason = EventType switch
                {
                    TransactionEventEnumType.Started => TriggerReasonEnumType.Authorized,
                    TransactionEventEnumType.Ended => TriggerReasonEnumType.StopAuthorized,
                    _ => TriggerReasonEnumType.MeterValuePeriodic
                },
                Timestamp = timestamp.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                EVSE = EvseId is > 0 ? new EVSEType { Id = EvseId.Value } : null!,
                IdToken = string.IsNullOrEmpty(IdTag) ? null! : new IdTokenType { IdToken = IdTag, Type = IdTokenEnumType.ISO14443 },
                TransactionInfo = new TransactionType { TransactionId = TransactionUid },
                MeterValue = MeterWh.HasValue
                    ? new List<MeterValueType>
                    {
                        new()
                        {
                            Timestamp = timestamp,
                            SampledValue = new List<SampledValueType>
                            {
                                new()
                                {
                                    Value = MeterWh.Value,
                                    Measurand = MeasurandEnumType.Energy_Active_Import_Register,
                                    Context = context,
                                    UnitOfMeasure = new UnitOfMeasureType { Unit = "Wh" }
                                }
                            }
                        }
                    }
                    : null!
            };
        }
    }

    public static class RoamingTransactionObserverExtensions
    {
        public static Task<AuthorizationStatusEnumType?> OnTransactionEventAsync(this IRoamingTransactionObserver observer, string chargePointId,
            RoamingTransactionEvent transactionEvent, CancellationToken cancellationToken = default) =>
            observer.OnTransactionEventAsync(chargePointId, transactionEvent.ToTransactionEventRequest(), cancellationToken);
    }
}
