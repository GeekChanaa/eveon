using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
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
            var users = this._context.Users.Where(u => u.PartnerID != null).AsQueryable();
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

        public async Task<List<UserNameDto>> GetSupportUserNames()
        {
            var users = await _context.Users.Where(u => u.Role == UserRole.Support).ToListAsync();
            return _mapper.Map<List<User>,List<UserNameDto>>(users);
        }

        public async Task<string> GetUserEmailByID(int userID)
        {
            return (await _context.Users.FirstOrDefaultAsync(u => u.ID == userID)).Email;
        }

        public async Task<bool> UserExists(string email)
        {
            if (await _context.Users.AnyAsync(x => x.Email == email))
            {
                return true;
            }
            return false;
        }

        public async Task<User> GetUser(int id)
        {
            // Getting The user and some of its navigation properties
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == id);
            return user;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public IQueryable<UserListDto> GetUsers(GlobalParams globalParams)
        {
            var users = GetAllAsync(globalParams).ProjectTo<UserListDto>(_mapper.ConfigurationProvider);
            return users;
        }

        public async Task<UserDashboardDisplayInformationsDto> GetUserDashboardDisplayInformations(int userID)
        {
            var user = await GetByIdAsync(userID);
            return _mapper.Map<User,UserDashboardDisplayInformationsDto>(user);
        }

        public async Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto)
        {
            var user = await GetByIdAsync(userID);
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.Email = userDto.Email;
            user.Phone = userDto.Phone;
            user.Role = userDto.Role;
            user.Birthday = userDto.Birthday;
            user.IsEmailVerified = userDto.IsEmailVerified;
            user.IsPhoneNumberVerified = userDto.IsPhoneNumberVerified;
            user.SuspendedAt = userDto.SuspendedAt;

            await Update(user);
        }
    }
}