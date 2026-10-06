using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;

namespace VoltaXApi.Services;

/// <summary>
/// Unverified email / password accounts may sign in, but cannot spend money: starting a
/// charge or topping up a card first requires a verified address. Google sign ins are
/// verified by Google (email_verified) and flagged verified when the account is created.
/// </summary>
public interface IEmailVerificationGuard
{
    Task EnsureVerifiedAsync(int userID);
}

public class EmailVerificationGuard(VoltaXApiDbContext context) : IEmailVerificationGuard
{
    public async Task EnsureVerifiedAsync(int userID)
    {
        var verified = await context.Users.Where(u => u.ID == userID).Select(u => (bool?)u.IsEmailVerified).SingleOrDefaultAsync();
        if (verified != true)
            throw new EmailNotVerifiedException();
    }

    public static void EnsureVerified(User user)
    {
        if (!user.IsEmailVerified)
            throw new EmailNotVerifiedException();
    }
}
