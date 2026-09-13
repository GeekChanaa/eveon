using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
  public interface IPartnerAuthService
  {
    Task PartnerResetPasswordRequest(string email);
    Task<LoginResultDto> Login(string email, string password, string ipAddress, string? userAgent = null);

    /// <summary>
    /// Signs an existing partner in from an external (Google) identity.
    /// Partner accounts are provisioned by an administrator, so this never creates one.
    /// </summary>
    Task<LoginResultDto> ExternalLogin(ExternalUserInfoDto externalUser, AuthProviderEnum provider, string? ipAddress = null, string? userAgent = null);

  }
}