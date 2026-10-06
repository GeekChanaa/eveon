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
        public const int MaxFailedAttempts = 5;
        public static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

        private readonly IMapper _mapper;
        public LoginAttemptRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<bool> IsLockedOut(string ipAddress)
        {
            var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);
            return loginAttempt != null && loginAttempt.LockoutEndTime > DateTime.UtcNow;
        }

        /// <summary>
        /// Counts a failure inside a fixed window that opens on the first failure. The row's
        /// CreatedAt marks the window start, so a stale row (window over and no lockout running)
        /// is removed and a fresh one opens the next window.
        /// Returns true when this failure is the one that triggers the lockout.
        /// </summary>
        public async Task<bool> LoginAttemptFailed(string ipAddress)
        {
            var now = DateTime.UtcNow;
            var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);

            if (loginAttempt != null && loginAttempt.LockoutEndTime <= now && loginAttempt.CreatedAt.Add(AttemptWindow) <= now)
            {
                // Hard delete: the soft delete in SaveChanges would leave one dead row per window.
                await _context.LoginAttempts.Where(x => x.ID == loginAttempt.ID).ExecuteDeleteAsync();
                _context.Entry(loginAttempt).State = EntityState.Detached;
                loginAttempt = null;
            }

            if (loginAttempt == null)
            {
                _context.LoginAttempts.Add(new LoginAttempt { IpAddress = ipAddress, FailedAttempts = 1 });
                await _context.SaveChangesAsync();
                return false;
            }

            loginAttempt.FailedAttempts++;

            bool lockedNow = loginAttempt.FailedAttempts >= MaxFailedAttempts && loginAttempt.LockoutEndTime <= now;
            if (lockedNow)
                loginAttempt.LockoutEndTime = now.Add(LockoutDuration);

            await _context.SaveChangesAsync();
            return lockedNow;
        }
    }
}
