using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Data
{
    public interface IUserRepository : IRepository<User>
    {
        Task<Boolean> UserEmailExists(string Email);
        Task<Boolean> UserPhoneExists(string Phone);
        Task<User?> FindUserByEmail(string Email);
        Task<string> GenerateResetPasswordTokenForUser(string Email);
        Task<List<DebitCardListingDto>> GetUserDebitCards(int UserId);
        Task<List<UserNameDto>> GetUserNames();
        Task<List<UserNameDto>> GetPartnerNames();
        Task<List<UserNameDto>> GetUserNamesByName(string name);
        Task<bool> IsEmailUnique(string email);
        Task<bool> IsPhoneUnique(string phone);
    }
}