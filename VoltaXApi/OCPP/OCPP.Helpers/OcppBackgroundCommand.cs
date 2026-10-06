using VoltaXApi.OCPP.Exceptions;

namespace VoltaXApi.OCPP.Helpers
{
    /// <summary>
    /// Sends a CSMS-initiated command without awaiting it inside the processing of a charger's own CALL:
    /// the command waits for the charger's answer, which the charger cannot send while its CALL is open.
    /// The command runs in its own DI scope (the CALL's scope is disposed when the CALL is answered).
    /// </summary>
    public static class OcppBackgroundCommand
    {
        public static void Run(
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            string command,
            string chargePointId,
            Func<IServiceProvider, Task> send)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await send(scope.ServiceProvider);
                }
                catch (TimeoutException ex)
                {
                    logger.LogWarning(ex, "{Command} to {ChargePointId} timed out", command, chargePointId);
                }
                catch (OcppCallErrorException ex)
                {
                    logger.LogWarning(ex, "{Command} rejected by {ChargePointId}: {ErrorCode} {ErrorDescription}",
                        command, chargePointId, ex.ErrorCode, ex.ErrorDescription);
                }
                catch (WebSocketNotFoundException ex)
                {
                    logger.LogWarning(ex, "{Command} not sent: {ChargePointId} is not connected", command, chargePointId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Command} to {ChargePointId} failed", command, chargePointId);
                }
            });
        }
    }
}
