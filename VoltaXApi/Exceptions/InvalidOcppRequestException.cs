namespace VoltaXApi.Exceptions;
public class InvalidOcppRequestException : Exception
{
    public InvalidOcppRequestException(string message) : base(message) { }
}