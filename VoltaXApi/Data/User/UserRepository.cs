using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Bogus.DataSets;
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
            return await PhoneExists(phone);
        }

        /// <summary>
        /// Phone numbers are unique across accounts. The caller may pass any of the shapes
        /// PhoneHelper accepts; comparison happens on the stored "+212XXXXXXXXX" form.
        /// </summary>
        public async Task<bool> PhoneExists(string phone, int? excludeUserID = null)
        {
            string normalized = PhoneHelper.Normalize(phone);
            if (normalized == null)
                return false;

            return await this._context.Users
                .AnyAsync(u => u.Phone == normalized && (excludeUserID == null || u.ID != excludeUserID));
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
            var user = await _context.Users.Include(u => u.DebitCards)
                .FirstOrDefaultAsync(u => u.ID == UserID);

            if (user == null)
                return new List<DebitCardListingDto>();

            var result = new List<DebitCardListingDto>();

            foreach (var card in user.DebitCards.Where(c => !c.IsDeleted))
            {
                var dto = new DebitCardListingDto
                {
                    ID = card.ID,
                    UserID = card.UserID ?? 0,
                    CardNumberHidden = Mask(card.CardNumber),
                    NameHidden = Mask(card.Name),
                    Type = DetermineCardType(card.CardNumber)
                };

                result.Add(dto);
            }

            return result;
        }

        private DebitCardTypeEnum DetermineCardType(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return DebitCardTypeEnum.Generic;

            // Clean the card number (remove spaces)
            var cleanNumber = cardNumber.Replace(" ", "");

            // Visa cards start with 4
            if (cleanNumber.StartsWith("4"))
                return DebitCardTypeEnum.Visa;

            // Mastercard starts with 51-55 or ranges 2221-2720
            if (cleanNumber.StartsWith("5") && cleanNumber.Length > 1)
            {
                var secondDigit = int.Parse(cleanNumber.Substring(1, 1));
                if (secondDigit >= 1 && secondDigit <= 5)
                    return DebitCardTypeEnum.Mastercard;
            }

            // Check for Mastercard's 2-series range
            if (cleanNumber.StartsWith("2") && cleanNumber.Length >= 4)
            {
                var prefix = int.Parse(cleanNumber.Substring(0, 4));
                if (prefix >= 2221 && prefix <= 2720)
                    return DebitCardTypeEnum.Mastercard;
            }

            // Default to Generic for any other patterns
            return DebitCardTypeEnum.Generic;
        }

        private string Mask(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Assuming you want to keep first and last character visible
            if (value.Length <= 2)
                return value;

            // Keep first and last characters visible, mask the rest with asterisks
            return value[0] + new string('*', value.Length - 2) + value[value.Length - 1];
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
            return await PhoneExists(phone);
        }

        public async Task<List<UserNameDto>> GetSupportUserNames()
        {
            var users = await _context.Users.Where(u => u.Role.Name == "SUPPORT").ToListAsync();
            return _mapper.Map<List<User>, List<UserNameDto>>(users);
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
            var user = await _context.Users
            .Where(u => u.ID == userID)
            .Select(u => new UserDashboardDisplayInformationsDto
            {
                ID = u.ID,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                RoleID = u.RoleID,
                Role = u.Role.Name,
                PartnerID = u.PartnerID,
                Partner = u.Partner.Name,
                EvModel = u.ElectricVehicleModel == null ? "" : u.ElectricVehicleModel.Make + " " + u.ElectricVehicleModel.Model,
                Birthday = u.Birthday,
                Gender = u.Gender,
                IsEmailVerified = u.IsEmailVerified,
                IsPhoneNumberVerified = u.IsPhoneNumberVerified,
                SuspendedAt = u.SuspendedAt,
            }).FirstOrDefaultAsync();

            return user;
        }

        public async Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto)
        {
            var user = await GetByIdAsync(userID);

            if (await PhoneExists(userDto.Phone, userID))
            {
                throw new ValidationException("This phone number is already used by another account");
            }

            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.Email = userDto.Email;
            user.Phone = userDto.Phone;
            user.RoleID = userDto.RoleID;
            user.Birthday = userDto.Birthday;
            user.IsEmailVerified = userDto.IsEmailVerified;
            user.IsPhoneNumberVerified = userDto.IsPhoneNumberVerified;
            user.SuspendedAt = userDto.SuspendedAt;
            user.ElectricVehicleModelID = userDto.ElectricVehicleModelID;
            user.PartnerID = userDto.PartnerID ?? null;

            await Update(user);
        }

        public async Task<UserListDto> GetUserInformations(int userID)
        {
            var user = await _context.Users
                        .Select(u => new UserListDto
                        {
                            ID = u.ID,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            Email = u.Email,
                            Gender = u.Gender,
                            City = u.City,
                            Birthday = u.Birthday,
                            Phone = u.Phone,
                            PartnerName = u.PartnerID == null ? null : u.Partner.Name,
                            IsEmailVerified = u.IsEmailVerified,
                            IsPhoneNumberVerified = u.IsPhoneNumberVerified,
                            RoleName = u.Role.Name,
                            ImageUrl = u.Image.Url
                        })
                        .FirstOrDefaultAsync(u => u.ID == userID);
            return user;
        }

        public async Task<User> CreateUser(UserForRegisterDto userForRegisterDto, byte[] passwordHash, byte[] passwordSalt)
        {
            var user = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                Phone = userForRegisterDto.Phone,
                RoleID = 2
            };
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            user.IsEmailVerified = false;
            user.EmailVerificationToken = AuthHelper.GenerateVerificationToken();

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }


        public IQueryable<User> GetAdminsQueryable()
        {
            return _context.Users.Where(u => u.Role.Name == "ADMIN");
        }
        public IQueryable<User> GetSupportsQueryable()
        {
            return _context.Users.Where(u => u.Role.Name == "SUPPORT");
        }
        public IQueryable<User> GetCustomersQueryable()
        {
            return _context.Users.Where(u => u.Role.Name != "ADMIN" && u.Role.Name != "SUPPORT");
        }
        public IQueryable<User> GetPartnersQueryable()
        {
            return _context.Users.Where(u => u.PartnerID != null);
        }

        public async Task<string> GetUserPhoneNumber(int userID)
        {
            return (await _context.Users.FirstOrDefaultAsync(u => u.ID == userID)).Phone;
        }

        public async Task<List<UserNameDto>> GetRoleUsers(int roleID)
        {
            return await _context.Users.Where(u => u.RoleID == roleID).Select(u => new UserNameDto
            {
                ID = u.ID,
                FullName = u.FullName
            }).ToListAsync();
        }
    }
}