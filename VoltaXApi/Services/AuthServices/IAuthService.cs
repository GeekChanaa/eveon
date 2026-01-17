using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IAuthService
    {
      Task<User> Register(UserForRegisterDto userForRegisterDto);
      Task<LoginResultDto> Login(string email, string password, string ipAddress);
      Task<bool> VerifyEmail(string email, string token);
      Task<string?> VerifyPhoneNumber(string email, string token);
      Task SendPhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto);
      Task CreateEmailVerificationToken(int userID);
      Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto);
      Task ResetPasswordRequest(string email);
      Task ResetPasswordRequestForMobile(string email);
      Task<string> VerifyResetPasswordCodeForMobile(UserResetPasswordForMobileDto userResetPasswordForMobileDto);
    }
}