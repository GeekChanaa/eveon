using Bogus;
using System.Collections.Generic;
using System.Linq;
using VoltaXApi.Models;
using VoltaXApi.Data;

public static class UserSeeder
{
    public static async Task<List<User>> Seed(int number, VoltaXApiDbContext context)
    {
        // User repo 
        IRepository<User> _userRepo = new Repository<User>(context);
        var fakeUsers = new Faker<User>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.PasswordHash, f => f.Random.Bytes(64))
            .RuleFor(u => u.PasswordSalt, f => f.Random.Bytes(64))
            .RuleFor(u => u.RoleID, f => 1) 
            .RuleFor(u => u.Orders, f => new List<Order>())
            .FinishWith((f, u) =>
            {
                Console.WriteLine($"User created. Id={u.ID}, Email={u.Email}");
            });
        var testUsers = AuthRepository.CreateTestUsers();
        var users = fakeUsers.Generate(number).ToList();
        users.AddRange(testUsers);
        await _userRepo.AddRangeAsync(users);
        return users;
    }
}
