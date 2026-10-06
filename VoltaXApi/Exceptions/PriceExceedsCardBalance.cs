namespace VoltaXApi.Exceptions;

public sealed class PriceExceedsCardBalance : Exception
{
    public PriceExceedsCardBalance(double currentBalance, double minimumRequiredBalance)
        : base($"A minimum card balance of {minimumRequiredBalance:0.00} MAD is required to start charging. Your current balance is {currentBalance:0.00} MAD.")
    {
        CurrentBalance = currentBalance;
        MinimumRequiredBalance = minimumRequiredBalance;
    }

    public double CurrentBalance { get; }
    public double MinimumRequiredBalance { get; }
}
