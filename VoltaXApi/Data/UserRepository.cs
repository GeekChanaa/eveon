using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
namespace VoltaXApi.Data
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly IMapper _mapper;
        public UserRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<Boolean> UserEmailExists(string email)
        {
            return await this._context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<Boolean> UserPhoneExists(string phone)
        {
            return await this._context.Users.AnyAsync(u => u.Phone == phone);
        }

        public async Task<User?> FindUserByEmail(string email)
        {
            return await this._context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<string> GenerateResetPasswordTokenForUser(string email)
        {
            var user = await this.FindUserByEmail(email);
            if (user == null)
                throw new Exception("User not found");

            string token = TokenGenerator.GenerateToken();

            user.ResetPasswordToken = token;

            await this.Update(user);
            return user.ResetPasswordToken;
        }

        public async Task<List<DebitCardListingDto>> GetUserDebitCards(int UserID)
        {
            var user = await _context.Users.Include(u => u.DebitCards).FirstOrDefaultAsync(u => u.ID == UserID);

            if (user != null)
            {
                return _mapper.ProjectTo<DebitCardListingDto>(user.DebitCards.AsQueryable()).ToList();
            }
            else
            {
                throw new Exception("User does not exist");
            }
        }

        public async Task<List<UserNameDto>> GetUserNames()
        {
            var users = this._context.Users.AsQueryable();
            return await _mapper.ProjectTo<UserNameDto>(users).ToListAsync();
        }

        public async Task<List<UserNameDto>> GetPartnerNames()
        {
            var users = this._context.Users.Where(u => u.Role == UserRole.Partner).AsQueryable();
            return await _mapper.ProjectTo<UserNameDto>(users).ToListAsync();
        }

        public async Task<List<UserNameDto>> GetUserNamesByName(string name)
        {
            var users = this._context.Users.Where(u => (u.FirstName + " " + u.LastName).Contains(name)).AsQueryable();
            return await _mapper.ProjectTo<UserNameDto>(users).ToListAsync();
        }

        public async Task<bool> IsEmailUnique(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsPhoneUnique(string phone)
        {
            return await _context.Users.AnyAsync(u => u.Phone == phone);
        }
    }
}