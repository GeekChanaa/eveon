using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Controllers
{
    /// <summary>
    /// Turns the outcome of a CSMS command into the HTTP answer of the OCPP controllers:
    /// 200 { message, status, response } with the charger's real response, 409 when the charger is not connected,
    /// 504 when it did not answer in time, 502 when it answered with a CALLERROR.
    /// </summary>
    public static class OcppCommandResult
    {
        public static async Task<IActionResult> Run<TResponse>(string action, Func<Task<TResponse>> send,
            IDictionary<string, object?>? extra = null)
        {
            try
            {
                var response = await send();
                return Ok(action, response, extra);
            }
            catch (Exception ex) when (ToError(ex) is { } error)
            {
                return error;
            }
        }

        public static IActionResult Ok<TResponse>(string action, TResponse response, IDictionary<string, object?>? extra = null)
        {
            // Serialized like the OCPP wire format (camelCase, enum names), so `response` mirrors what the charger sent.
            var json = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            var element = JsonDocument.Parse(json).RootElement.Clone();
            string? status = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String
                ? s.GetString()
                : null;

            var body = new Dictionary<string, object?>
            {
                ["message"] = status == null ? $"{action}: the charger answered." : $"{action}: charger answered {status}",
                ["status"] = status,
                ["response"] = element
            };
            if (extra != null)
                foreach (var (key, value) in extra) body[key] = value;
            return new OkObjectResult(body);
        }

        /// <summary>Maps the transport exceptions; null for anything else (left to the global handling).</summary>
        public static IActionResult? ToError(Exception ex) => ex switch
        {
            OcppProtocolNotSupportedException => Error(StatusCodes.Status400BadRequest, ex.Message, "NotSupportedByProtocol"),
            // Must stay before WebSocketNotFoundException (its base type): the charger may be fine,
            // the instance owning its socket just cannot be reached right now.
            VoltaXApi.ScaleOut.ScaleOutUnavailableException => Error(StatusCodes.Status503ServiceUnavailable, "Charger routing is temporarily unavailable.", "Unavailable"),
            WebSocketNotFoundException => Error(StatusCodes.Status409Conflict, "The charge point is not connected.", "NotConnected"),
            TimeoutException => Error(StatusCodes.Status504GatewayTimeout, ex.Message, "Timeout"),
            OcppCallErrorException callError => new ObjectResult(new
            {
                message = callError.Message,
                error = callError.Message,
                status = "CallError",
                errorCode = callError.ErrorCode
            }) { StatusCode = StatusCodes.Status502BadGateway },
            _ => null
        };

        private static IActionResult Error(int statusCode, string message, string status) =>
            new ObjectResult(new { message, error = message, status }) { StatusCode = statusCode };
    }
}
