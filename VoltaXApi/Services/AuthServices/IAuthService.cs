using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IAuthService
    {
      Task<User> Register(UserForRegisterDto userForRegisterDto);
      Task<User> Login(string email, string login, string ipAddress);
      bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
      void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);      
      Task<bool> VerifyEmail(string email, string token);
      Task<bool> VerifyPhoneNumber(string email, string token);
      Task CreatePhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto);
      Task CreateEmailVerificationToken(int userID);
      Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto);
      Task ResetPasswordRequest(string email);
    }
}