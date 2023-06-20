using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Data
{
    public interface IUserRepository : IRepository<User>
    {
        Task<Boolean> UserEmailExists(string Email);
    }
}