using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Data
{
    public interface IUserRepository : IRepository<User>
    {
        Task<Boolean> UserEmailExists(string Email);
        Task<User?> FindUserByEmail(string Email);
        Task GenerateResetPasswordTokenForUser(string Email);
        Task<List<DebitCardListingDto>> GetUserDebitCards(int UserId);
    }
}