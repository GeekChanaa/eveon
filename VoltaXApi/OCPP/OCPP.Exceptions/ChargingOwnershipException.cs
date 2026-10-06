namespace VoltaXApi.OCPP.Exceptions
{
    /// <summary>The caller tried to charge with a card/token that is not their own active card (answered with 403).</summary>
    public class ChargingOwnershipException : Exception
    {
        public ChargingOwnershipException(string message) : base(message) { }
    }
}
