using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
  public interface IPartnerAuthService
  {
    Task PartnerResetPasswordRequest(string email);
    Task<LoginResultDto> Login(string email, string password, string ipAddress);

  }
}