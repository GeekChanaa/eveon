using System.Threading.Tasks;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IAuthRepository
    {
        Task<User> Register(User user, string password);
        Task<User> Login(string email, string login);
        Task<bool> UserExists(string email);
        Task<User> GetUser(int id);
        bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
        void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
    }
}