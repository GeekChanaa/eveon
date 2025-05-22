using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Data
{
    public class LoginAttemptRepository : Repository<LoginAttempt>, ILoginAttemptRepository
    {
        private readonly IMapper _mapper;
        public LoginAttemptRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task LoginAttemptFailed(string ipAddress)
        {
            var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);
            if (loginAttempt == null)
            {
                loginAttempt = new LoginAttempt { IpAddress = ipAddress, FailedAttempts = 1 };
                _context.LoginAttempts.Add(loginAttempt);
                await _context.SaveChangesAsync();
            }
            else
            {
                loginAttempt.FailedAttempts++;
                if (loginAttempt.FailedAttempts >= 5)
                {
                    loginAttempt.LockoutEndTime = DateTime.UtcNow.AddMinutes(5);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}