namespace VoltaXApi.Exceptions;
public class NoEVConnectedException : Exception
{
    public NoEVConnectedException(string message) : base(message) { }
}