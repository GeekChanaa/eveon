namespace VoltaXApi.Exceptions;
public class OcppProtocolException : Exception
{
    public OcppProtocolException(string message) : base(message) { }
}