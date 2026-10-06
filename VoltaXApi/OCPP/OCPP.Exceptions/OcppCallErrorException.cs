
namespace VoltaXApi.OCPP.Exceptions
{
    /// <summary>The charger answered a CSMS request with a CALLERROR frame.</summary>
    public class OcppCallErrorException : Exception
    {
        public string? ErrorCode { get; }

        public string? ErrorDescription { get; }

        /// <summary>Raw JSON of the CALLERROR details object.</summary>
        public string? ErrorDetails { get; }

        public OcppCallErrorException(string? errorCode, string? details)
            : base($"Charger returned {errorCode ?? "an error"}{(string.IsNullOrWhiteSpace(details) ? "" : ": " + details)}")
        {
            ErrorCode = errorCode;
            ErrorDescription = details;
        }

        public OcppCallErrorException(string? errorCode, string? errorDescription, string? errorDetails)
            : this(errorCode, errorDescription)
        {
            ErrorDetails = errorDetails;
        }
    }
}
