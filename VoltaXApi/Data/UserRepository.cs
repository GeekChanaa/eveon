using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Data
{
    public class UserRepository : Repository<User> , IUserRepository
    {
        public UserRepository(VoltaXApiDbContext context) : base(context)
        {

        }
        
        public async Task<Boolean> UserEmailExists(string email)
        {
            return await ._context.Users.AnyAsync(u=> u.Email == email);
        }
    }   
}