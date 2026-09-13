using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IAuthService
    {
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