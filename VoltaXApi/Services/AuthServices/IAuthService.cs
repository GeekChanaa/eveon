using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IAuthService
    {
      Task<object> RequestPhoneLogin(string number, string ip);
      Task<LoginResultDto?> VerifyPhoneLogin(string number, string challengeId, string code, string ip, string? userAgent);
      Task<User> Register(UserForRegisterDto userForRegisterDto);
      /// <summary>
      /// Signs a user in. <paramref name="identifier"/> is either an email address or a
      /// phone number ("0610610614" or "+212610610614").
      /// </summary>
      Task<LoginResultDto> Login(string identifier, string password, string ipAddress, string? userAgent = null);
      Task<bool> VerifyEmail(string email, string token);
      Task<string?> VerifyPhoneNumber(string email, string token);
      Task SendPhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto);
      Task CreateEmailVerificationToken(int userID);
      Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto);
      Task ResetPasswordRequest(string email);
      Task ResetPasswordRequestForMobile(string email);
      Task<string> VerifyResetPasswordCodeForMobile(UserResetPasswordForMobileDto userResetPasswordForMobileDto);
      Task ResetPassword(string email, string token, string newPassword);
      Task ResendVerificationEmail(int userID);

      /// <summary>Second step of a 2FA sign in: swaps the challenge token and a TOTP / recovery code for a session.</summary>
      Task<LoginResultDto> CompleteTwoFactorLogin(string twoFactorToken, string code, string? ipAddress, string? userAgent);

      /// <summary>
      /// Signs a user in from an external (Google) identity, creating the local account on first use.
      /// </summary>
      Task<LoginResultDto> ExternalLogin(ExternalUserInfoDto externalUser, AuthProviderEnum provider, string? ipAddress = null, string? userAgent = null);

      /// <summary>
      /// Attaches an external identity to an already authenticated local account.
      /// </summary>
      Task LinkExternalAccount(int userID, ExternalUserInfoDto externalUser, AuthProviderEnum provider);

      /// <summary>
      /// Detaches the external identity. Refused when it would leave the account with no way to sign in.
      /// </summary>
      Task UnlinkExternalAccount(int userID, AuthProviderEnum provider);

      Task<LinkedAccountsDto> GetLinkedAccounts(int userID);
    }
}
