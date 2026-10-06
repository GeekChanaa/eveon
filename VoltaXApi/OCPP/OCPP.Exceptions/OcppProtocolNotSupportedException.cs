namespace VoltaXApi.OCPP.Exceptions
{
  /// <summary>The command (or one of its options) does not exist in the OCPP version the charger speaks. Answered with HTTP 400.</summary>
  public class OcppProtocolNotSupportedException : Exception
  {
      public OcppProtocolNotSupportedException(string message) : base(message)
      {
      }

      public static OcppProtocolNotSupportedException ForCommand(string action, string protocolVersion) =>
          new($"{action} is not supported by OCPP {protocolVersion.Replace("ocpp", "")} chargers.");
  }
}
