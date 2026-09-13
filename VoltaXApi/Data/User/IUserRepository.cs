using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;
using Org.BouncyCastle.Bcpg;

namespace VoltaXApi.Data
{
    public interface IUserRepository : IRepository<User>
    {
        Task<Boolean> UserEmailExists(string Email);
        Task<Boolean> UserPhoneExists(string Phone);

        /// <summary>
        /// True when the number already belongs to an account other than
        /// <paramref name="excludeUserID"/>. Accepts any of the shapes PhoneHelper takes.
        /// </summary>
        Task<bool> PhoneExists(string phone, int? excludeUserID = null);
        Task<User?> FindUserByEmail(string Email);
        Task<string> GenerateResetPasswordTokenForUser(string Email);
        Task<List<DebitCardListingDto>> GetUserDebitCards(int UserId);
        Task<List<UserNameDto>> GetUserNames();
        Task<List<UserNameDto>> GetPartnerNames();
        Task<List<UserNameDto>> GetUserNamesByName(string name);
        Task<bool> IsEmailUnique(string email);
        Task<bool> IsPhoneUnique(string phone);
        Task<List<UserNameDto>> GetSupportUserNames();
        Task<string> GetUserEmailByID(int userID);
        Task<User?> GetUserByEmail(string email);
        Task<User> GetUser(int id);
        Task<User> CreateUser(UserForRegisterDto userForRegisterDto, byte[] PasswordHash, byte[] PasswordSalt);
        Task<bool> UserExists(string email);
        IQueryable<UserListDto> GetUsers(GlobalParams globalParams);
        Task<UserDashboardDisplayInformationsDto> GetUserDashboardDisplayInformations(int userID);
        Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto);
        Task<UserListDto> GetUserInformations(int userID);
        IQueryable<User> GetAdminsQueryable();
        IQueryable<User> GetSupportsQueryable();
        IQueryable<User> GetCustomersQueryable();
        IQueryable<User> GetPartnersQueryable();
        Task<string> GetUserPhoneNumber(int userID);
        Task<List<UserNameDto>> GetRoleUsers(int roleID);
    }
}