using System.Threading.Tasks;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;
using VoltaXApi.Services;

namespace VoltaXApi.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly VoltaXApiDbContext _context;
        public AuthRepository(VoltaXApiDbContext context)
        {
            this._context = context;
        }

        public static List<User> CreateTestUsers()
        {
            string password = "test";
            byte[] passHash , passSalt;
            AuthService.CreatePasswordHashStatic(password,out passHash,out passSalt);
            var customer = new User{
                FirstName = "test",
                LastName = "test",
                Email = "customer@gmail.com",
                Phone = "06606060",
                PasswordHash = passHash,
                PasswordSalt = passSalt,
                IsEmailVerified = true,
                IsPhoneNumberVerified = true,
                Role = UserRole.Customer
            };

            var admin = new User{
                FirstName = "test",
                LastName = "test",
                Email = "admin@gmail.com",
                Phone = "0610614476",
                PasswordHash = passHash,
                PasswordSalt = passSalt,
                IsEmailVerified = true,
                IsPhoneNumberVerified = true,
                Role = UserRole.Admin
            };

            var partner = new User{
                FirstName = "test",
                LastName = "test",
                Email = "partner@gmail.com",
                Phone = "0610614475",
                PasswordHash = passHash,
                PasswordSalt = passSalt,
                IsEmailVerified = true,
                IsPhoneNumberVerified = true,
                PartnerID = null,
                Role = UserRole.Support
            };

            var listUsers = new List<User>();
            listUsers.Add(customer);
            listUsers.Add(admin);
            listUsers.Add(partner);
            return listUsers;
        }
    }
}