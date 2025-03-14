namespace VoltaXApi.Exceptions;
public class ConnectorUnavailableException : Exception
{
    public ConnectorUnavailableException(string message) : base(message) { }
}