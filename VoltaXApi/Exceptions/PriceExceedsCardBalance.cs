namespace VoltaXApi.Exceptions;

public sealed class PriceExceedsCardBalance : Exception
{
    public PriceExceedsCardBalance(decimal currentBalance, decimal minimumRequiredBalance)
        : base($"A minimum card balance of {minimumRequiredBalance:0.00} MAD is required to start charging. Your current balance is {currentBalance:0.00} MAD.")
    {
        CurrentBalance = currentBalance;
        MinimumRequiredBalance = minimumRequiredBalance;
    }

    public decimal CurrentBalance { get; }
    public decimal MinimumRequiredBalance { get; }
}
