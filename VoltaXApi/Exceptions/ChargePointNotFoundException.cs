namespace VoltaXApi.Exceptions;
public class ChargePointNotFoundException : Exception
{
    public ChargePointNotFoundException(string message) : base(message) { }
}