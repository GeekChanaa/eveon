using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Bogus;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class UserSeeder
    {
        public async static Task<List<User>> Seed(int count, VoltaXApiDbContext dbContext)
        {
            IRepository<User> repo = new Repository<User>(dbContext);
            var userFaker = new Faker<User>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.Password, "test");

            var users = userFaker.Generate(count);

            foreach (var user in users)
            {
                CreatePasswordHash(user.Password, out var passwordHash, out var passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            await repo.AddRangeAsync(users);
            return users;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }

}
