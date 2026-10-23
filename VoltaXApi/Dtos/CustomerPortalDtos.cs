using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    /// <summary>A page of results with its total, in the response body.</summary>
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

    public sealed record MyProfileDto(string FirstName, string LastName, string Email, string? Phone, DateTime? Birthday,
        string? ImageUrl, bool IsEmailVerified, bool IsPhoneNumberVerified, DateTime MemberSince);

    /// <summary>
    /// The caller's running session. <see cref="ChargePointName"/> is the charger's OCPP id and <see cref="TransactionUid"/>
    /// its transaction id: together they let the owner stop the session (EVDriver/RequestStopTransactionMobile, which
    /// checks ownership again). Live updates arrive on the charging session hub.
    /// </summary>
    public sealed record MyActiveSessionDto(int Id, string? StationName, string? ChargePointName, string? Connector,
        DateTime StartDate, double ChargedMinutes, double ChargedKwhs, decimal CurrentCost, string? TransactionUid, decimal? CardBalance);

    public sealed record MyOverviewDto(string FirstName, decimal WalletBalance, int CardCount, MyActiveSessionDto? ActiveSession,
        int SessionsThisMonth, double KwhThisMonth, decimal SpentThisMonth, int SessionsTotal, double KwhTotal, decimal SpentTotal,
        string Currency);

    public sealed record MyCardDto(int Id, string MaskedNumber, string Last4, decimal Balance, DateTime ExpirationDate,
        CardStatusEnum Status, bool Blocked, bool Expired);

    /// <summary>A charging session as its owner sees it: amounts VAT included, no internal ids or operator costs.</summary>
    public sealed record MySessionDto(int Id, string? StationName, string? StationAddress, string? ChargePointName, string? Connector,
        DateTime StartDate, DateTime? EndDate, double ChargedMinutes, double IdleMinutes, double ChargedKwhs,
        decimal EnergyAmount, decimal TimeAmount, decimal FlatFeeAmount, decimal IdleAmount, decimal Total,
        ReasonEnumType StoppedReason, ChargingSessionStatusEnum Status, bool Active, bool InvoiceAvailable);

    public sealed record MySessionDetailDto(MySessionDto Session, decimal? PricePerKWh, decimal? PricePerMinute, decimal? PricePerIdleMinute,
        decimal? FlatFee, decimal VatRate, decimal TotalExclVat, decimal Vat, decimal? BalanceBefore, decimal? BalanceAfter, string? CardLast4,
        string Currency);

    public sealed record MyWalletMovementDto(int Id, DateTime CreatedAt, WalletTransactionType Type, decimal Amount, decimal BalanceAfter,
        string? Description, int? ChargingSessionId, int? OrderId, string CardLast4);

    public sealed record MyOrderDto(int Id, DateTime CreatedAt, decimal Amount, RechargeOrderStatus Status, string? Provider,
        DateTime? PaidAt, string? FailureReason, string CardLast4, bool InvoiceAvailable);

    public sealed class MySessionFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        /// <summary>active, completed or empty for all.</summary>
        public string? Status { get; set; }
        /// <summary>Matches station or charge point name.</summary>
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
