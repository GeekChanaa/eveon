namespace VoltaXApi.Exceptions;

/// <summary>Answered as 403 with code "email_not_verified" so clients can offer to resend the link.</summary>
public class EmailNotVerifiedException : Exception
{
    public const string Code = "email_not_verified";

    public EmailNotVerifiedException()
        : base("Please verify your email address before charging or topping up your card.") { }
}
