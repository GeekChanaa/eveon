using System.Buffers;
using System.Text;
using System.Text.Json;

namespace VoltaXApi.OCPP.Models
{
    public enum OcppMessageType
    {
        Call = 2,
        CallResult = 3,
        CallError = 4
    }

    /// <summary>One OCPP-J RPC frame. <see cref="Payload"/> is the raw JSON object (CALL / CALLRESULT payload or CALLERROR details).</summary>
    public sealed record OcppFrame(
        OcppMessageType MessageType,
        string UniqueId,
        string? Action,
        string Payload,
        string? ErrorCode = null,
        string? ErrorDescription = null);

    /// <summary>
    /// Result of parsing a text frame. When <see cref="Frame"/> is null the frame was malformed:
    /// <see cref="UniqueId"/> is set when the message id could still be read and <see cref="MessageType"/>
    /// when the message type could (a malformed CALLRESULT/CALLERROR is never answered).
    /// </summary>
    public sealed class OcppFrameParseResult
    {
        public OcppFrame? Frame { get; init; }
        public string? UniqueId { get; init; }
        public int? MessageType { get; init; }
        public OcppError Error { get; init; }
        public string? ErrorDescription { get; init; }

        public bool Success => Frame != null;
    }

    /// <summary>OCPP-J framing: [2,"id","Action",{}], [3,"id",{}], [4,"id","ErrorCode","ErrorDescription",{}].</summary>
    public static class OcppJson
    {
        public const int MaxUniqueIdLength = 36;

        public static OcppFrameParseResult Parse(string text)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(text);
            }
            catch (JsonException)
            {
                return Fail(null, null, OcppError.RpcFrameworkError, "The message is not valid JSON.");
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                    return Fail(null, null, OcppError.RpcFrameworkError, "The message is not a JSON array.");

                var length = root.GetArrayLength();
                string? uniqueId = length > 1 && root[1].ValueKind == JsonValueKind.String ? root[1].GetString() : null;
                if (string.IsNullOrEmpty(uniqueId)) uniqueId = null;

                if (length < 3)
                    return Fail(uniqueId, null, OcppError.RpcFrameworkError, "The message has too few elements.");

                if (root[0].ValueKind != JsonValueKind.Number || !root[0].TryGetInt32(out var messageType))
                    return Fail(uniqueId, null, OcppError.RpcFrameworkError, "The message type id is not a number.");

                if (uniqueId == null)
                    return Fail(null, messageType, OcppError.RpcFrameworkError, "The message id is missing or not a string.");
                if (uniqueId.Length > MaxUniqueIdLength)
                    return Fail(uniqueId, messageType, OcppError.RpcFrameworkError, $"The message id is longer than {MaxUniqueIdLength} characters.");

                switch (messageType)
                {
                    case (int)OcppMessageType.Call:
                        if (length != 4 || root[2].ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(root[2].GetString()))
                            return Fail(uniqueId, messageType, OcppError.RpcFrameworkError, "A CALL must be [2, \"messageId\", \"action\", {payload}].");
                        if (root[3].ValueKind != JsonValueKind.Object)
                            return Fail(uniqueId, messageType, OcppError.FormationViolation, "The CALL payload is not a JSON object.");
                        return Ok(new OcppFrame(OcppMessageType.Call, uniqueId, root[2].GetString()!.Trim(), root[3].GetRawText()));

                    case (int)OcppMessageType.CallResult:
                        if (length != 3 || root[2].ValueKind != JsonValueKind.Object)
                            return Fail(uniqueId, messageType, OcppError.RpcFrameworkError, "A CALLRESULT must be [3, \"messageId\", {payload}].");
                        return Ok(new OcppFrame(OcppMessageType.CallResult, uniqueId, null, root[2].GetRawText()));

                    case (int)OcppMessageType.CallError:
                        // The details object is mandatory in the spec; some chargers leave it out, so it is tolerated.
                        if (length < 4 || length > 5 || root[2].ValueKind != JsonValueKind.String || root[3].ValueKind != JsonValueKind.String
                            || (length == 5 && root[4].ValueKind != JsonValueKind.Object))
                            return Fail(uniqueId, messageType, OcppError.RpcFrameworkError, "A CALLERROR must be [4, \"messageId\", \"errorCode\", \"errorDescription\", {details}].");
                        return Ok(new OcppFrame(OcppMessageType.CallError, uniqueId, null,
                            length == 5 ? root[4].GetRawText() : "{}", root[2].GetString(), root[3].GetString()));

                    default:
                        return Fail(uniqueId, messageType, OcppError.MessageTypeNotSupported, $"Message type {messageType} is not supported.");
                }
            }
        }

        public static string SerializeCall(string uniqueId, string action, string? payloadJson) =>
            Write(w =>
            {
                w.WriteNumberValue((int)OcppMessageType.Call);
                w.WriteStringValue(uniqueId);
                w.WriteStringValue(action);
                w.WriteRawValue(PayloadOrEmpty(payloadJson));
            });

        public static string SerializeCallResult(string uniqueId, string? payloadJson) =>
            Write(w =>
            {
                w.WriteNumberValue((int)OcppMessageType.CallResult);
                w.WriteStringValue(uniqueId);
                w.WriteRawValue(PayloadOrEmpty(payloadJson));
            });

        public static string SerializeCallError(string uniqueId, string errorCode, string? errorDescription, string? detailsJson = null) =>
            Write(w =>
            {
                w.WriteNumberValue((int)OcppMessageType.CallError);
                w.WriteStringValue(uniqueId);
                w.WriteStringValue(errorCode);
                w.WriteStringValue(errorDescription ?? string.Empty);
                w.WriteRawValue(PayloadOrEmpty(detailsJson));
            });

        private static string PayloadOrEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? "{}" : json;

        private static string Write(Action<Utf8JsonWriter> body)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                body(writer);
                writer.WriteEndArray();
            }
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        private static OcppFrameParseResult Ok(OcppFrame frame) => new() { Frame = frame, UniqueId = frame.UniqueId, MessageType = (int)frame.MessageType };

        private static OcppFrameParseResult Fail(string? uniqueId, int? messageType, OcppError error, string description) =>
            new() { UniqueId = uniqueId, MessageType = messageType, Error = error, ErrorDescription = description };
    }
}
