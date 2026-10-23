using System.Text.Json;
using VoltaX.Tests.Infrastructure;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Handlers;
using VoltaXApi.OCPP.Models;

namespace VoltaX.Tests.Ocpp;

/// <summary>OCPP-J framing: CALL / CALLRESULT / CALLERROR arrays, malformed frames and the error code spellings per version.</summary>
public class OcppFramingTests
{
    [Fact]
    public void Frames_are_serialized_as_OCPP_J_arrays()
    {
        Assert.Equal("[2,\"abc\",\"Reset\",{\"type\":\"OnIdle\"}]", OcppJson.SerializeCall("abc", "Reset", "{\"type\":\"OnIdle\"}"));
        Assert.Equal("[3,\"abc\",{\"status\":\"Accepted\"}]", OcppJson.SerializeCallResult("abc", "{\"status\":\"Accepted\"}"));
        Assert.Equal("[3,\"abc\",{}]", OcppJson.SerializeCallResult("abc", null));
        Assert.Equal("[4,\"abc\",\"NotImplemented\",\"Unknown action\",{}]", OcppJson.SerializeCallError("abc", "NotImplemented", "Unknown action"));
        Assert.Equal(5, ScriptedWebSocket.Elements(OcppJson.SerializeCallError("a\"b", "GenericError", "quote \" and \\ backslash")).Length); // escaped
    }

    [Fact]
    public void A_CALL_is_parsed_with_whitespace_and_nested_payloads()
    {
        var call = OcppJson.Parse(" [ 2 , \"id-1\" , \"Heartbeat\" , { } ] ");
        Assert.True(call.Success);
        Assert.Equal((OcppMessageType.Call, "id-1", "Heartbeat", "{ }"), (call.Frame!.MessageType, call.Frame.UniqueId, call.Frame.Action, call.Frame.Payload));

        var nested = OcppJson.Parse("[2,\"id-2\",\"DataTransfer\",{\"data\":\"[3,\\\"x\\\",{}]\",\"a\":[1,{\"b\":2}]}]");
        Assert.True(nested.Success);
        Assert.Equal(2, JsonDocument.Parse(nested.Frame!.Payload).RootElement.GetProperty("a")[1].GetProperty("b").GetInt32());
    }

    [Fact]
    public void Replies_are_parsed()
    {
        var result = OcppJson.Parse("[3,\"id-3\",{\"status\":\"Accepted\"}]");
        Assert.True(result.Success);
        Assert.Equal(OcppMessageType.CallResult, result.Frame!.MessageType);
        Assert.Null(result.Frame.Action);
        Assert.Contains("Accepted", result.Frame.Payload);

        var error = OcppJson.Parse("[4,\"id-4\",\"NotSupported\",\"nope\",{\"x\":1}]");
        Assert.True(error.Success);
        Assert.Equal(("NotSupported", "nope", "{\"x\":1}"), (error.Frame!.ErrorCode, error.Frame.ErrorDescription, error.Frame.Payload));
        Assert.True(OcppJson.Parse("[4,\"id-5\",\"GenericError\",\"\"]").Success); // without details
    }

    public static TheoryData<string, string?, OcppError> MalformedFrames => new()
    {
        { "not json", null, OcppError.RpcFrameworkError },
        { "{\"a\":1}", null, OcppError.RpcFrameworkError },                          // object instead of array
        { "[2]", null, OcppError.RpcFrameworkError },                                // too short, no id
        { "[2,\"id\"]", "id", OcppError.RpcFrameworkError },                         // too short
        { "[\"2\",\"id\",\"Heartbeat\",{}]", "id", OcppError.RpcFrameworkError },    // message type as a string
        { "[2,5,\"Heartbeat\",{}]", null, OcppError.RpcFrameworkError },             // numeric message id
        { "[2,\"id\",\"Heartbeat\",[]]", "id", OcppError.FormationViolation },       // payload not an object
        { "[2,\"id\",\"Heartbeat\"]", "id", OcppError.RpcFrameworkError },           // no payload
        { "[2,\"id\",7,{}]", "id", OcppError.RpcFrameworkError },                    // action not a string
        { "[9,\"id\",{}]", "id", OcppError.MessageTypeNotSupported },
        { "[2,\"" + new string('x', 37) + "\",\"Heartbeat\",{}]", new string('x', 37), OcppError.RpcFrameworkError }, // id longer than 36
        { "[3,\"id\",\"Heartbeat\",{}]", "id", OcppError.RpcFrameworkError },        // CALLRESULT with an action
    };

    [Theory]
    [MemberData(nameof(MalformedFrames))]
    public void Malformed_frames_are_refused_with_their_id_when_readable(string text, string? id, OcppError error)
    {
        var parsed = OcppJson.Parse(text);

        Assert.False(parsed.Success);
        Assert.Equal(id, parsed.UniqueId);
        Assert.Equal(error, parsed.Error);
    }

    [Fact]
    public void Error_codes_use_each_version_s_spelling()
    {
        Assert.Equal("FormatViolation", OcppErrors.ToWireName(OcppError.FormationViolation, OcppProtocols.Ocpp201));
        Assert.Equal("FormationViolation", OcppErrors.ToWireName(OcppError.FormationViolation, OcppProtocols.Ocpp16));
        Assert.Equal("OccurenceConstraintViolation", OcppErrors.ToWireName(OcppError.OccurrenceConstraintViolation, OcppProtocols.Ocpp16));
        Assert.Equal("RpcFrameworkError", OcppErrors.ToWireName(OcppError.RpcFrameworkError, OcppProtocols.Ocpp201));
        Assert.Equal(OcppError.FormationViolation, OcppErrors.Parse("FormatViolation"));
        Assert.Equal(OcppError.OccurrenceConstraintViolation, OcppErrors.Parse(ErrorCodes.OccurenceConstraintViolation));
        Assert.Equal(OcppError.GenericError, OcppErrors.Parse("Bogus"));
    }
}
