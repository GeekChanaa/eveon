using System.Threading.Tasks;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly VoltaXApiDbContext _context;
        public AuthRepository(VoltaXApiDbContext context)
        {
            this._context = context;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            // Email verification initial configuration
            user.IsEmailVerified = false;
            user.EmailVerificationToken = GenerateVerificationToken();

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Creating a standard recharge card for the user
            Card userCard = new Card
            {
                CardNumber = GenerateCardNumber(),
                CardType = CardTypeEnum.Standard,
                ExpirationDate = DateTime.Now.AddYears(2),
                MaxCount = 1,
                Status = CardStatusEnum.Inactive,
                Balance = 100,
                Note = "Initial card",
                UserID = user.ID
            };

            _context.Cards.Add(userCard);

            return user;
        }

        private string GenerateCardNumber()
        {
            // Generate a new GUID
            Guid guid = Guid.NewGuid();
            
            // Convert the GUID to a string and remove the hyphens
            string cardNumber = guid.ToString().Replace("-", "");
            
            // Return the first 16 characters of the card number
            return cardNumber.Substring(0, 16);
        }


        // Generating a verification token 
        private string GenerateVerificationToken()
        {
            Random random = new Random();
            const int tokenLength = 6;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // only digits
            return new string(Enumerable.Repeat(chars, tokenLength)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        // Verifying the email
        public async Task<bool> VerifyEmail(string email, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null || user.EmailVerificationToken != token)
                return false;

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null; // clear the token
            await _context.SaveChangesAsync();

            return true;
        }

        // Verifying the phone number
        public async Task<bool> VerifyPhoneNumber(string phoneNumber, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Phone == phoneNumber);
            if (user == null || user.PhoneVerificationToken != token)
                return false;

            user.IsPhoneNumberVerified = true;
            user.PhoneVerificationToken = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<User> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return null;
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            return user;
        }

        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        // Unicity of Email
        public async Task<bool> UserExists(string email)
        {
            if (await _context.Users.AnyAsync(x => x.Email == email))
            {
                return true;
            }
            return false;
        }

        // Getting user
        public async Task<User> GetUser(int id)
        {
            // Getting The user and some of its navigation properties
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == id);
            return user;
        }

        // Creating phone verification token and updating the user
        public async Task CreatePhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == addPhoneNumberDto.Email);
            user.Phone = addPhoneNumberDto.Phone;
            user.PhoneVerificationToken = this.GenerateVerificationToken();
            user.IsPhoneNumberVerified = false;
            
            this._context.Set<User>().Entry(user).State = EntityState.Modified;
            await this._context.SaveChangesAsync();
        }
    }
}