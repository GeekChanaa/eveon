namespace VoltaXApi.Exceptions;
public class AuthorizationFailedException : Exception
{
    public AuthorizationFailedException(string message) : base(message) { }
}